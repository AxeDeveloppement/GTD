# 🚀 Guide de déploiement — PersonalGTD

Ce guide explique comment configurer le déploiement automatique de l'application **PersonalGTD.Web** sur **GitHub Pages** via GitHub Actions.

---

## 1. 🔑 Configurer les Secrets GitHub

Les clés Supabase ne doivent **jamais** être committées dans le code source. Elles sont injectées automatiquement lors du déploiement par le pipeline CI/CD.

### Comment ajouter les secrets :

1. Rends-toi sur la page de ton dépôt GitHub :  
   👉 `https://github.com/AxeDeveloppement/GTD`

2. Clique sur **Settings** (onglet en haut à droite du dépôt)

3. Dans le menu gauche, va dans **Security → Secrets and variables → Actions**

4. Clique sur **"New repository secret"** et crée les deux secrets suivants :

| Nom du secret | Valeur |
|---|---|
| `SUPABASE_URL` | `https://jbolffzgbwqystewrxng.supabase.co` |
| `SUPABASE_KEY` | *(ta clé `anon` Supabase)* |

> **⚠️ ATTENTION** : Ne partage jamais ta clé Supabase publiquement. Une fois saisie dans les Secrets GitHub, elle est chiffrée et ne sera plus visible.

---

## 2. ⚙️ Activer GitHub Pages

1. Dans **Settings**, va dans **Pages** (menu gauche)

2. Dans **Source**, sélectionne :
   - **Branch** : `gh-pages`
   - **Folder** : `/ (root)`

3. Clique sur **Save**

> La branche `gh-pages` sera créée automatiquement lors du premier déploiement réussi.

---

## 3. 🔄 Déclencher le déploiement

Le déploiement se lance automatiquement à chaque `git push` sur la branche `main`.

Tu peux aussi le déclencher manuellement :
1. Va dans l'onglet **Actions** de ton dépôt
2. Clique sur **"Deploy to GitHub Pages"** dans le menu gauche
3. Clique sur **"Run workflow"** → **"Run workflow"**

---

## 4. 🌍 URL de l'application déployée

Une fois déployée, l'application sera accessible à :
```
https://axedeveloppement.github.io/GTD/
```

---

## 5. 🔍 Ce que fait le pipeline

Le fichier `.github/workflows/deploy.yml` exécute automatiquement :

1. **Checkout** du code source
2. **Installation** de .NET 9
3. **Injection** des secrets Supabase dans `appsettings.json` (via `sed`)
4. **Compilation** en Release de `PersonalGTD.Web`
5. **Correction** du `base href` pour le sous-chemin GitHub Pages (`/GTD/`)
6. **Création** du `404.html` (copie de `index.html`) pour que le routage Blazor fonctionne même sur les routes directes
7. **Ajout** du fichier `.nojekyll` pour désactiver le traitement Jekyll de GitHub
8. **Déploiement** sur la branche `gh-pages`

---

## 6. ⚠️ Problèmes courants

| Problème | Solution |
|---|---|
| Page blanche après déploiement | Vérifier que les secrets sont bien configurés et relancer le workflow |
| Erreur 404 sur les routes | Le `404.html` doit être présent (généré automatiquement) |
| "Page not found" sur GitHub Pages | Attendre 2-3 minutes et activer Pages dans Settings |

---

## 7. 📱 Installer l'application Android depuis GitHub

L'application mobile Android est compilée automatiquement par le workflow **Build Android APK** (`.github/workflows/build-android.yml`).

### Méthode 1 — Télécharger l'APK depuis les Artifacts (rapide)

1. Va dans l'onglet **Actions** de ton dépôt GitHub :
   👉 `https://github.com/AxeDeveloppement/GTD/actions`

2. Clique sur **"Build Android APK"** dans le menu gauche

3. Ouvre le dernier run réussi (marqué d'un ✓ vert)

4. En bas de la page, dans la section **Artifacts**, clique sur `PersonalGTD-Android.apk` pour télécharger le fichier `.apk`

5. Transfère le fichier sur ton téléphone Android (via USB, email, ou cloud)

6. Sur ton téléphone, ouvre le fichier `.apk` et accepte l'installation

> **⚠️ Note** : Si ton téléphone refuse l'installation, active **"Autoriser les sources inconnues"** dans `Paramètres → Sécurité`.

### Méthode 2 — Télécharger depuis une Release (recommandé)

Une release Android est automatiquement créée à chaque commit contenant `[release]` dans le message.

1. Va sur la page **Releases** du dépôt :
   👉 `https://github.com/AxeDeveloppement/GTD/releases`

2. Clique sur la dernière release (tag `android-XXX`)

3. Télécharge le fichier `com.axeldeveloppement.gtd.apk` dans **Assets**

4. Transfère-le sur ton téléphone et installe-le

### Méthode 3 — Compiler localement

Si tu préfères construire l'APK toi-même :

```bash
# Cloner le dépôt
git clone https://github.com/AxeDeveloppement/GTD.git
cd GTD

# Installer la workload MAUI
dotnet workload install maui

# Restaurer et publier
dotnet restore PersonalGTD.Android/PersonalGTD.Android.csproj
dotnet publish PersonalGTD.Android/PersonalGTD.Android.csproj -c Release -f net9.0-android -o ./publish

# L'APK se trouve dans ./publish/com.axeldeveloppement.gtd.apk
```

### Déclencher un build manuellement

1. Va dans **Actions → Build Android APK**
2. Clique sur **"Run workflow"** → **"Run workflow"**

---

## 8. 🔧 Configuration Supabase pour l'app mobile

L'application mobile utilise les mêmes clés Supabase que la version web. Assure-toi que les secrets `SUPABASE_URL` et `SUPABASE_KEY` sont configurés dans le dépôt (voir section 1).
