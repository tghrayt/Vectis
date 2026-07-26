# Deploiement VM avec GitHub Actions

Ce deploiement cible une VM Ubuntu avec Docker et Docker Compose deja installes.

## Architecture

- GitHub Actions compile et teste l'application.
- GitHub Actions construit une image Docker `vectis-app`.
- L'image est copiee sur la VM en SSH puis chargee avec `docker load`.
- `docker compose` demarre :
  - `vectis-web` sur le port public `8080` par defaut ;
  - `vectis-db`, un PostgreSQL dedie a Vectis, non expose publiquement.

URL temporaire sans domaine :

```text
http://51.210.40.78:8080
```

## Preparation VM

Sur la VM :

```bash
sudo mkdir -p /opt/vectis
sudo chown ubuntu:ubuntu /opt/vectis
```

Verifier Docker :

```bash
docker --version
docker compose version
```

## Cle SSH pour GitHub Actions

Depuis ton PC, cree une cle dediee au deploy si tu n'en as pas encore :

```bash
ssh-keygen -t ed25519 -C "github-actions-vectis" -f vectis_deploy_key
```

Ajoute la cle publique sur la VM :

```bash
ssh-copy-id -i vectis_deploy_key.pub ubuntu@51.210.40.78
```

Si `ssh-copy-id` n'est pas disponible, copie le contenu de `vectis_deploy_key.pub` dans :

```bash
~/.ssh/authorized_keys
```

Dans GitHub, ajoute la cle privee `vectis_deploy_key` dans le secret `VM_SSH_KEY`.

## Secrets GitHub requis

Dans GitHub :

`Settings` -> `Secrets and variables` -> `Actions` -> `New repository secret`

Secrets obligatoires :

```text
VM_HOST=51.210.40.78
VM_USER=ubuntu
VM_PORT=22
VM_SSH_KEY=<cle privee SSH de deploy>
POSTGRES_PASSWORD=<mot de passe postgres fort>
```

Secrets SMTP optionnels :

```text
SMTP_HOST=
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_FROM_EMAIL=
```

Variables GitHub optionnelles :

```text
APP_PORT=8080
SMTP_ENABLED=false
SMTP_PORT=587
SMTP_FROM_NAME=Vectis
SMTP_ENABLE_SSL=true
```

## Premier deploiement

Le workflow se lance automatiquement sur chaque push vers `main`.

Tu peux aussi le lancer manuellement :

`Actions` -> `Deploy to VM` -> `Run workflow`

## Verification apres deploiement

Sur ton PC :

```bash
curl http://51.210.40.78:8080/health
```

Sur la VM :

```bash
cd /opt/vectis
docker compose -f docker-compose.prod.yml --env-file .env ps
docker logs --tail 100 vectis-web
docker logs --tail 100 vectis-db
```

## Commandes utiles

Redemarrer :

```bash
cd /opt/vectis
docker compose -f docker-compose.prod.yml --env-file .env restart
```

Arreter :

```bash
cd /opt/vectis
docker compose -f docker-compose.prod.yml --env-file .env down
```

Sauvegarder PostgreSQL :

```bash
docker exec vectis-db pg_dump -U vectis vectis > vectis-backup.sql
```

## Domaine et HTTPS

Pour l'instant, l'application est prevue en HTTP par IP.

Quand le domaine sera pret, on ajoutera :

- Caddy ou Nginx ;
- certificat Let's Encrypt ;
- redirection HTTPS ;
- exposition publique sur `80` et `443` au lieu de `8080`.
