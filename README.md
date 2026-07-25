# Vectis

Vectis est une application web responsive de gestion du lait maternel tire, conserve et consomme. Le MVP vise une version utilisable localement par des parents : creation de compte, famille, bebe, tirages, contenants, stock, preparation de biberon, consommation partielle, historique et alertes d'expiration dans l'application.

## Analyse du besoin

Les acteurs principaux sont le parent administrateur et le parent/accompagnant autorise. Le MVP couvre les parcours essentiels : creer l'espace familial, creer le bebe, enregistrer un tirage, repartir le lait en contenants, suivre le stock disponible, preparer un biberon depuis plusieurs contenants, enregistrer la quantite bue et consulter l'historique.

Regles metier prioritaires :

- une quantite restante ne devient jamais negative ;
- la somme des contenants ne depasse pas le tirage total ;
- les contenants consommes, jetes ou expires ne sont pas inclus dans le stock ;
- les prelevements diminuent automatiquement le stock ;
- le stock est trie par expiration estimee ;
- les actions importantes alimentent un journal d'audit ;
- les donnees sont separees par famille ;
- les regles de conservation sont configurables.

Point d'attention : les durees de conservation ne sont pas des conseils medicaux. Elles sont des parametres d'organisation a adapter selon les recommandations sanitaires pertinentes.

## Proposition technique

Stack retenue pour cette premiere version :

- .NET 10 ;
- ASP.NET Core Razor Pages ;
- domaine metier separe dans `Vectis.Domain` ;
- stockage JSON local dans `src/Vectis.Web/App_Data/vectis-data.json` ;
- authentification cookie ;
- hachage de mot de passe PBKDF2 ;
- tests metier via un runner console sans dependance NuGet externe.

Ce choix permet de demarrer vite avec un monolithe modulaire, maintenable, simple a lancer sur Windows, et evolutif vers PostgreSQL/EF Core, API REST complete, PWA offline avancee ou application mobile native.

## Architecture

```text
Vectis.sln
src/
  Vectis.Domain/     Modeles, etat applicatif, regles metier
  Vectis.Web/        Razor Pages, auth, stockage JSON, interface responsive
tests/
  Vectis.Tests/      Tests critiques des regles metier
```

Le domaine ne depend pas du web. Le projet web charge l'etat JSON, applique les operations via `VectisEngine`, puis sauvegarde. Cette separation permettra de remplacer le stockage par une base relationnelle avec migrations sans reecrire les regles.

## UX et UI

Navigation principale :

- Tableau ;
- Tirage ;
- Stock ;
- Biberon ;
- Historique ;
- Regles.

Les ecrans sont responsives, compatibles mobile, tablette et ordinateur. Le mode sombre suit le parametre systeme. Les actions frequentes ont des valeurs par defaut pour tester rapidement le scenario d'acceptation : 180 ml repartis en 100 ml et 80 ml, puis biberon de 120 ml avec 90 ml bus.

## Plan de developpement

MVP :

- inscription/connexion ;
- famille et premier bebe ;
- tirage et creation de deux contenants ;
- stock disponible et tri par expiration ;
- preparation de biberon depuis plusieurs contenants ;
- consommation partielle ;
- historique et audit ;
- regles de conservation configurables ;
- tests metier.

Version 1 :

- vraie base PostgreSQL avec migrations ;
- API REST documentee ;
- invitations et permissions fines ;
- decongelation complete ;
- export CSV ;
- PWA offline avec file locale et resolution de conflits simple.

Version 2 :

- notifications push ;
- QR code et etiquettes ;
- statistiques avancees ;
- synchronisation multi-appareils robuste ;
- applications mobiles packagees Android/iOS.

Futur :

- connexion Google/Apple ;
- biometrie ;
- export PDF ;
- multi-bebes avance ;
- integrations objets connectes.

## Lancement local

Prerequis :

- .NET SDK 10 installe.

Commandes :

```powershell
cd C:\Users\Tghrayt\source\repos\Vectis
dotnet build
dotnet run --project src/Vectis.Web
```

Ouvre ensuite l'URL affichee par ASP.NET Core, souvent `https://localhost:7xxx` ou `http://localhost:5xxx`.

Compte de demonstration :

- email : `demo@vectis.local`
- mot de passe : `Demo123!`

## Tests

```powershell
cd C:\Users\Tghrayt\source\repos\Vectis
dotnet run --project tests/Vectis.Tests
```

Les tests verifient notamment :

- rejet d'une repartition de contenants superieure au tirage ;
- diminution automatique du stock lors d'un biberon ;
- tracabilite des sources du biberon ;
- exclusion des contenants expires ;
- suivi du reste non bu ;
- isolation entre familles.

## Donnees et configuration

Les donnees locales sont stockees dans :

```text
src/Vectis.Web/App_Data/vectis-data.json
```

Ce fichier est ignore par Git. Pour repartir a zero, arrete l'application puis supprime ce fichier.

Les regles de conservation sont modifiables dans l'ecran `Regles`.

## Limites actuelles

- Stockage JSON local, pas encore de base de donnees relationnelle.
- Pas encore de notification mobile push.
- Pas encore de vraie synchronisation hors connexion multi-utilisateur.
- Pas encore d'impression d'etiquettes ni QR code.
- API REST publique limitee a venir en V1.

## Deploiement

Pour publier le MVP web :

```powershell
dotnet publish src/Vectis.Web -c Release
```

En production, remplacer le stockage JSON par une base de donnees, configurer HTTPS, logs securises, sauvegardes, variables d'environnement, retention RGPD et secrets hors depot.
