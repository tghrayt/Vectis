# Vectis Mobile

Enveloppe mobile Capacitor pour publier Vectis sur Android et iOS sans dupliquer l'application web.

## Principe

- La version web continue de tourner en production.
- L'app mobile charge l'URL de production definie dans `capacitor.config.json`.
- Les changements fonctionnels faits dans `src/Vectis.Web` sont donc disponibles sur web et mobile.
- Seules les fonctions natives, l'icone, le splash screen et la publication store restent propres au dossier mobile.

## URL de production

Par defaut:

```text
https://vectis.51-210-40-78.sslip.io
```

Quand le vrai domaine OVH sera pret, remplace `server.url` dans `capacitor.config.json`, puis lance:

```bash
npm run sync
```

## Commandes

```bash
npm install
npm run sync
npm run open:android
```

Sur iOS, il faut un Mac avec Xcode:

```bash
npm run open:ios
```

## Versions

Le projet est fige en Capacitor `7.6.8`, compatible avec Node.js 20. Capacitor 8 existe, mais demande Node.js 22; on pourra migrer quand l'environnement local et CI seront prets.

`npm run doctor` peut afficher `Xcode is not installed` sur Windows. C'est normal: Android peut etre prepare sur Windows, iOS doit etre ouvert et compile sur macOS avec Xcode/CocoaPods.

## Maintenance

Pour une fonctionnalite produit classique, modifier `src/Vectis.Web` suffit. Re-synchroniser le projet mobile seulement quand on change:

- l'URL de production;
- l'icone ou le splash screen;
- une permission native;
- un plugin Capacitor;
- la configuration Android/iOS.
