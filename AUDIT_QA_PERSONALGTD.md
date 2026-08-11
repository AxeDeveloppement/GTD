# Rapport d'Audit QA - PersonalGTD

**Date:** 10 août 2026  
**Auditeur:** Ingénieur QA & Code Reviewer  
**Portée:** Analyse complète de la solution PersonalGTD (Blazor WASM + MAUI Android + Supabase)

---

## 1. Vue d'ensemble du Projet

| Caractéristique | Détail |
|---|---|
| Framework | .NET 9.0 |
| Architecture | Blazor WebAssembly (Web) + MAUI Hybrid (Android) + Shared Library |
| Backend | Supabase (PostgreSQL + Auth) |
| Solution | 3 projets: `PersonalGTD.Shared`, `PersonalGTD.Web`, `PersonalGTD.Android` |
| Cible GTD | Gestion de tâches personnelles avec workflow Inbox -> Next Action -> Done/Abandoned |

---

## 2. Résumé Exécutif

**Note Globale: 5.5/10** - Projet fonctionnel mais présentant **6 vulnérabilités critiques** et **8 problèmes majeurs** nécessitant une correction avant toute mise en production.

| Catégorie | Critique | Majeur | Mineur |
|---|---|---|---|
| Sécurité | 4 | 2 | 0 |
| Performance | 1 | 2 | 1 |
| Fiabilité | 1 | 2 | 2 |
| Qualité du Code | 0 | 2 | 3 |

---

## 3. Vulnérabilités Critiques (Priorité IMMEDIATE)

### CRIT-01: Clé API Supabase exposée en clair dans le binaire
**Fichier:** [`SupabaseConfig.cs`](PersonalGTD.Shared/SupabaseConfig.cs:14)  
**Sévérité:** CRITIQUE  

La clé anon Supabase est codée en dur comme valeur par défaut:
```csharp
public static string Key => !string.IsNullOrEmpty(_envKey)
    ? _envKey!
    : "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // Clé exposée!
```
Cette clé est visible par décompilation du binaire WASM (code client 100% visible). Bien qu'il s'agisse d'une clé `anon` Supabase, elle doit être considérée comme sensible.

**Recommandation:**
- Implémenter les RLS (Row Level Security) stricts sur toutes les tables Supabase
- Ne jamais faire confiance au client pour le filtrage des données
- Rotater la clé immédiatement si elle a été commitée dans un dépôt public

---

### CRIT-02: NotificationWorker récupère TOUTES les tâches de TOUS les utilisateurs
**Fichier:** [`NotificationWorker.cs`](PersonalGTD.Android/Platforms/Android/NotificationWorker.cs:57)  
**Sévérité:** CRITIQUE  

```csharp
var response = Task.Run(async () => await client.From<GtdItem>().Get()).GetAwaiter().GetResult();
var tasks = response.Models; // Toutes les tâches de TOUS les utilisateurs!
```
Le worker ne filtre pas par `owner_username`, récupérant ainsi l'intégralité de la table `tasks`. C'est une fuite de données massive.

**Recommandation:**
- Ajouter le filtre `.Filter("owner_username", Operator.Equals, username)` avant `.Get()`
- Récupérer le username depuis `Preferences.Default.Get("gtd_user", null)`

---

### CRIT-03: Credentials de démonstration en dur avec mots de passe prévisibles
**Fichier:** [`AuthService.cs`](PersonalGTD.Shared/Services/AuthService.cs:293)  
**Sévérité:** CRITIQUE  

```csharp
private static readonly Dictionary<string, string> DemoCredentials = new()
{
    { "Dounette", "Axel" },
    { "Axel", "Dounette" },
    { "demo", "demo" },
};
```
Ces credentials contournent entièrement l'authentification Supabase et permettent un accès sans session valide.

**Recommandation:**
- Supprimer ce mécanisme de login en dur pour la production
- Si un mode démo est nécessaire, le conditionner avec un `#if DEMO_MODE` et documenter explicitement

---

### CRIT-04: Update sans vérification de propriété (Take-over de tâche)
**Fichiers:** [`TaskService.cs`](PersonalGTD.Shared/Services/TaskService.cs:46), [`ProjectService.cs`](PersonalGTD.Shared/Services/ProjectService.cs:46), [`ContextService.cs`](PersonalGTD.Shared/Services/ContextService.cs:46)  
**Sévérité:** CRITIQUE  

```csharp
// TaskService.cs ligne 46
await _supabase.From<GtdItem>().Where(x => x.Id == item.Id).Update(item);
```
L'update ne filtre que par `Id` sans vérifier `owner_username`. Un utilisateur malveillant peut modifier n'importe quelle tâche en connaissant son GUID.

**Recommandation:**
- Ajouter `.Filter("owner_username", Operator.Equals, user)` à chaque opération Update
- Exemple: `await _supabase.From<GtdItem>().Where(x => x.Id == item.Id).Filter("owner_username", Operator.Equals, user).Update(item);`

---

## 4. Problèmes Majeurs

### MAJ-01: Blocking calls dans NotificationWorker (Task.Run + .GetResult())
**Fichier:** [`NotificationWorker.cs`](PersonalGTD.Android/Platforms/Android/NotificationWorker.cs:45)  

```csharp
Task.Run(async () => await client.InitializeAsync()).GetAwaiter().GetResult();
Task.Run(async () => await client.Auth.SetSession(...)).GetAwaiter().GetResult();
var response = Task.Run(async () => await client.From<GtdItem>().Get()).GetAwaiter().GetResult();
```
Ce pattern `Task.Run(...).GetAwaiter().GetResult()` est un anti-pattern qui:
- Crée des risques de deadlocks
- Gaspi les threads du pool
- Peut causer des ANR (Application Not Responding) sur Android

**Recommandation:** Utiliser `Worker` async natif ou augmenter le timeout du worker plutôt que de bloquer.

---

### MAJ-02: Placeholders non remplacés dans Program.cs Web
**Fichier:** [`Program.cs`](PersonalGTD.Web/Program.cs:30)  

```csharp
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "SUPABASE_URL_PLACEHOLDER";
var supabaseKey = builder.Configuration["Supabase:Key"] ?? "SUPABASE_KEY_PLACEHOLDER";
```
Si `appsettings.json` n'est pas correctement configuré, l'application utilisera des placeholders invalides.

**Recommandation:**
- Valider la présence des clés de configuration au démarrage
- Lancer une exception explicite si les valeurs sont manquantes

---

### MAJ-03: ReviewStateService sans persistance
**Fichier:** [`ReviewStateService.cs`](PersonalGTD.Shared/Services/ReviewStateService.cs:5)  

```csharp
private DateTime? _reviewDoneAt = null; // En mémoire uniquement
```
L'état de la revue hebdomadaire est perdu à chaque rechargement de page.

**Recommandation:** Persister `_reviewDoneAt` via `ISessionStorage` ou dans Supabase.

---

### MAJ-04: Double filtrage redondant (côté serveur + côté client)
**Fichiers:** [`TaskService.cs`](PersonalGTD.Shared/Services/TaskService.cs:22), [`ProjectService.cs`](PersonalGTD.Shared/Services/ProjectService.cs:22), [`ContextService.cs`](PersonalGTD.Shared/Services/ContextService.cs:22)  

```csharp
var response = await _supabase.From<GtdItem>().Filter("owner_username", ..., user).Get();
return response.Models.Where(x => x.OwnerUsername == user); // Redondant!
```
Le filtrage est fait deux fois, ce qui est inutile mais pas dangereux.

**Recommandation:** Supprimer le `.Where()` côté client une fois les RLS Supabase correctement configurés.

---

### MAJ-05: Mélange JSInterop et MAUI Storage dans AuthService
**Fichier:** [`AuthService.cs`](PersonalGTD.Shared/Services/AuthService.cs:45)  

```csharp
try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ...); } catch { }
try { Microsoft.Maui.Storage.Preferences.Default.Set(...); } catch { }
```
Le code tente les deux plateformes avec des `try/catch` vides, ce qui masque les erreurs silencieusement.

**Recommandation:** Utiliser l'abstraction `ISessionStorage` déjà existante au lieu de dupliquer la logique.

---

### MAJ-06: Delay arbitraire de 300ms dans InitializeAsync
**Fichier:** [`AuthService.cs`](PersonalGTD.Shared/Services/AuthService.cs:130)  

```csharp
await Task.Delay(300);
```
Ce délai est un hack pour attendre la synchronisation de Supabase.

**Recommandation:** Utiliser un mécanisme d'attente événementiel plutôt qu'un délai fixe.

---

## 5. Problèmes Mineurs

| ID | Fichier | Description |
|---|---|---|
| MIN-01 | [`AuthService.cs:76`](PersonalGTD.Shared/Services/AuthService.cs:76) | Exception catchée et logguée en console uniquement (invisible en production WASM) |
| MIN-02 | [`NotificationWorker.cs:90`](PersonalGTD.Android/Platforms/Android/NotificationWorker.cs:90) | Commentaire "Nouvelle version du channel" suggère un channel précédent non nettoyé |
| MIN-03 | [`Program.cs:7`](PersonalGTD.Web/Program.cs:7) | Console.WriteLine en production (bruit dans la console navigateur) |
| MIN-04 | [`GtdItem.cs:19`](PersonalGTD.Shared/Models/GtdItem.cs:19) | `CreatedAt` en `DateTime` (UTC) sans annotation de fuseau horaire explicite |
| MIN-05 | [`AuthState.cs`](PersonalGTD.Shared/Services/AuthState.cs:1) | Classe `AuthState` non utilisée nulle part dans la codebase (code mort) |

---

## 6. Points Positifs

1. **Architecture propre:** Séparation claire Shared/Web/Mobile avec injection de dépendances
2. **Modèles GTD cohérents:** Enum [`GtdStatus`](PersonalGTD.Shared/Models/GtdStatus.cs:3) bien structuré avec les 6 états classiques
3. **Abstraction ISessionStorage:** Bonne pratique pour supporter Web et Mobile sans duplication
4. **Notifications Android fonctionnelles:** Worker avec actions "Rappeler dans 1h" / "Ne plus rappeler" bien implémenté
5. **Deep Linking OAuth:** Flux d'authentification Google géré correctement sur Android via [`AuthCallbackActivity`](PersonalGTD.Android/Platforms/Android/AuthCallbackActivity.cs:7)

---

## 7. Plan de Correction Priorisé

### Phase 1 - Immédiat (Sécurité)
1. [ ] Ajouter le filtre `owner_username` dans [`NotificationWorker.cs`](PersonalGTD.Android/Platforms/Android/NotificationWorker.cs:57)
2. [ ] Ajouter le filtre `owner_username` sur les opérations Update dans les 3 services
3. [ ] Configurer les RLS Supabase pour chaque table
4. [ ] Supprimer ou conditionner les demo credentials

### Phase 2 - Court Terme (Stabilité)
5. [ ] Remplacer les blocking calls du NotificationWorker
6. [ ] Valider la configuration Supabase au démarrage de l'app Web
7. [ ] Persister ReviewStateService via ISessionStorage
8. [ ] Unifier le stockage de session via `ISessionStorage` dans `AuthService`

### Phase 3 - Moyen Terme (Qualité)
9. [ ] Nettoyer les Console.WriteLine en production
10. [ ] Supprimer le code mort (`AuthState`)
11. [ ] Ajouter des tests unitaires pour les services
12. [ ] Documenter la rotation des clés Supabase

---

## 8. Conclusion

Le projet PersonalGTD présente une architecture solide mais souffre de **failles de sécurité critiques** liées à une confiance excessive dans le client pour le filtrage des données. Les RLS (Row Level Security) côté Supabase sont indispensables avant toute mise en production. Le worker de notification Android constitue le risque le plus élevé car il expose les données de tous les utilisateurs sans filtrage.

**Action requise:** Corriger les 4 vulnérabilités critiques avant toute déploiement en environnement de production.
