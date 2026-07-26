# Deploiement VM avec GitHub Actions

Ce deploiement cible une VM Ubuntu avec Docker et Docker Compose deja installes.

## Architecture

- GitHub Actions compile et teste l'application.
- GitHub Actions construit une image Docker `vectis-app`.
- L'image est copiee sur la VM en SSH puis chargee avec `docker load`.
- `docker compose` demarre :
  - `vectis-proxy`, un reverse proxy Caddy public en `80/443` ;
  - `vectis-web`, accessible uniquement dans le reseau Docker ;
  - `vectis-db`, un PostgreSQL dedie a Vectis, non expose publiquement.

URL temporaire HTTPS sans domaine OVH :

```text
https://51-210-40-78.sslip.io
```

`sslip.io` fournit un DNS temporaire qui pointe automatiquement vers l'IP integree dans le nom d'hote.

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
APP_HOST=51-210-40-78.sslip.io
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
curl https://51-210-40-78.sslip.io/health
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

## Securiser PostgreSQL existant

Le PostgreSQL dedie a Vectis (`vectis-db`) n'est pas expose publiquement.

Si un ancien conteneur `postgres-db` expose encore `0.0.0.0:5432`, verifie d'abord son usage :

```bash
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Ports}}"
docker inspect postgres-db --format '{{ index .Config.Labels "com.docker.compose.project.working_dir" }}'
```

Si ce Postgres etait seulement un laboratoire et n'a pas besoin d'etre public, il faudra le recreer sans publication publique, ou avec `127.0.0.1:5432:5432`.

## Domaine definitif

Pour l'instant, l'application utilise HTTPS avec `sslip.io`.

Quand le domaine OVH sera pret, il suffira de :

- creer un `A record` vers `51.210.40.78` ;
- remplacer `APP_HOST=51-210-40-78.sslip.io` par ton domaine ;
- relancer le workflow GitHub Actions.
