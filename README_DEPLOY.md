# Deploiement VM avec k3s et GitHub Actions

Ce deploiement cible la VM Ubuntu `51.210.40.78`, ou k3s et Traefik sont deja installes.

## Architecture

Un seul reverse proxy public gere toutes les apps :

```text
Internet :80/:443
        |
      Traefik k3s
      /        \
ImmoPredict   Vectis
```

Vectis est deployee dans le namespace Kubernetes `vectis` :

- `vectis-web` : application ASP.NET Core ;
- `vectis-db` : PostgreSQL dedie avec volume persistant ;
- `Ingress` Traefik : route le host Vectis vers `vectis-web` ;
- `cert-manager` : demande le certificat Let's Encrypt ;
- `vectis-db-backup` : sauvegarde PostgreSQL quotidienne compressee.

URL temporaire avant le domaine OVH :

```text
https://vectis.51-210-40-78.sslip.io
```

`sslip.io` resout automatiquement ce nom vers `51.210.40.78`.

## GitHub Actions

Le workflow `.github/workflows/deploy-vm.yml` :

1. restaure, build et teste l'application ;
2. construit l'image Docker `vectis-app`;
3. copie l'image et les manifests k8s sur la VM ;
4. importe l'image dans k3s/containerd ;
5. installe `cert-manager` si absent ;
6. applique les secrets, le StatefulSet PostgreSQL, le Deployment web et l'Ingress Traefik ;
7. verifie `/health/ready` en HTTPS.

## Secrets GitHub requis

Dans GitHub :

`Settings` -> `Secrets and variables` -> `Actions`

Secrets :

```text
VM_HOST=51.210.40.78
VM_USER=ubuntu
VM_PORT=22
VM_SSH_KEY_B64=<cle privee SSH encodee en base64>
POSTGRES_PASSWORD=<mot de passe postgres fort>
```

Secrets SMTP optionnels :

```text
SMTP_HOST=
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_FROM_EMAIL=
```

Variables optionnelles :

```text
APP_HOST=vectis.51-210-40-78.sslip.io
SMTP_ENABLED=false
SMTP_PORT=587
SMTP_FROM_NAME=Vectis
SMTP_ENABLE_SSL=true
```

Pour generer `VM_SSH_KEY_B64` depuis PowerShell :

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes((Resolve-Path .\vectis_deploy_key)))
```

## Verification

Depuis ton PC :

```bash
curl https://vectis.51-210-40-78.sslip.io/health
curl https://vectis.51-210-40-78.sslip.io/health/ready
```

Sur la VM :

```bash
sudo k3s kubectl -n vectis get pods,svc,ingress,certificate,cronjob
sudo k3s kubectl -n vectis logs deployment/vectis-web --tail=100
sudo k3s kubectl -n cert-manager logs deployment/cert-manager --tail=100
```

## Commandes utiles

Redemarrer l'app :

```bash
sudo k3s kubectl -n vectis rollout restart deployment/vectis-web
```

Voir l'etat du deploiement :

```bash
sudo k3s kubectl -n vectis rollout status deployment/vectis-web
```

## Sauvegardes PostgreSQL

Une sauvegarde automatique tourne chaque nuit a `02:15 UTC`.

Les fichiers sont stockes sur la VM dans :

```text
/opt/vectis/backups
```

La retention supprime automatiquement les fichiers de plus de 14 jours.

Lister les sauvegardes :

```bash
sudo ls -lh /opt/vectis/backups
```

Lancer une sauvegarde manuelle :

```bash
sudo k3s kubectl -n vectis create job --from=cronjob/vectis-db-backup vectis-db-backup-manual-$(date -u +%Y%m%d%H%M%S)
```

Voir les jobs de sauvegarde :

```bash
sudo k3s kubectl -n vectis get jobs,pods -l app=vectis-db-backup
```

Suivre les logs de la derniere sauvegarde :

```bash
sudo k3s kubectl -n vectis logs -l app=vectis-db-backup --tail=100
```

Sauvegarder PostgreSQL directement :

```bash
sudo k3s kubectl -n vectis exec statefulset/vectis-db -- pg_dump -U vectis vectis > vectis-backup.sql
```

Restaurer une sauvegarde compressee demande une fenetre de maintenance :

```bash
gunzip -c /opt/vectis/backups/<fichier>.sql.gz | sudo k3s kubectl -n vectis exec -i statefulset/vectis-db -- psql -U vectis -d vectis
```

## Monitoring simple

Etat general :

```bash
sudo k3s kubectl -n vectis get pods,svc,ingress,certificate,cronjob,jobs
```

Evenements recents :

```bash
sudo k3s kubectl -n vectis get events --sort-by=.lastTimestamp
```

Logs applicatifs :

```bash
sudo k3s kubectl -n vectis logs deployment/vectis-web --tail=200
sudo k3s kubectl -n vectis logs deployment/vectis-web -f
```

Etat HTTPS :

```bash
sudo k3s kubectl -n vectis describe certificate vectis-web-tls
```

## Domaine OVH plus tard

Quand l'acces OVH sera revenu :

1. creer un `A record`, par exemple `vectis.tondomaine.com -> 51.210.40.78`;
2. modifier la variable GitHub `APP_HOST=vectis.tondomaine.com`;
3. relancer le workflow.

Traefik et cert-manager genereront un nouveau certificat Let's Encrypt pour le domaine definitif.
