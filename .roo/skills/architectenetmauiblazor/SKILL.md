---
name: architectenetmauiblazor
description: ègle d'or : L'agent doit distinguer Blazor WebAssembly de Blazor Hybrid.

Comportement : Il ne doit jamais modifier l'initialisation du BlazorWebView sans vérifier le XAML au préalable.

Garde-fou : Forcer l'agent à lire la documentation de Microsoft (ou à utiliser un outil de recherche web) avant de proposer un correctif sur des erreurs de démarrage.
---

# Architectenetmauiblazor

## Instructions

Règle de recherche documentaire :
Avant de proposer une solution architecturale ou de résoudre un bug lié à .NET MAUI, Blazor Hybrid ou C# 14, tu DOIS obligatoirement consulter la documentation officielle.
Utilise ton outil de recherche web avec la syntaxe suivante : site:learn.microsoft.com MAUI Blazor [ta recherche].
Ne te base jamais uniquement sur tes connaissances internes pour les versions récentes de .NET. Lis le contenu de la page officielle trouvée avant de générer le moindre code.
