## Review Assignment
[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/0E1ES0GJ)

[![CI Build & Test](https://github.com/arcreane/asp-pokemonteam/actions/workflows/dotnet-build-and-test.yml/badge.svg)](https://github.com/arcreane/asp-pokemonteam/actions/workflows/dotnet-build-and-test.yml)

# 🎮 Arène des Légendes - ASP.NET MVC

## Présentation
**Arène des Légendes** est une application multijoueur développée avec **ASP.NET MVC**. Elle permet aux joueurs d'affronter leurs adversaires dans un système de combat inspiré des jeux Pokémon. Le projet repose sur **Entity Framework** et **SQL Server** pour la gestion des données et applique les bonnes pratiques de développement ASP.NET.

---

## Objectifs
- Créer un **backend unique** gérant les combats multijoueurs
- Maintenir un code **modulaire** et bien structuré
- Concevoir une **base de données relationnelle** pour les utilisateurs, les Pokémon et l'inventaire
- Préparer la **mise en ligne** de l'application

---

## Architecture
- **Backend commun** : API REST ASP.NET MVC
- **Base de données** : SQL Server avec Entity Framework
- **Frontends individuels** : chaque étudiant réalise son interface
- **Gestion du code** : workflow Git avec branches dédiées

---

## Modèle de données
Les principales tables sont :
- `user_auth` : utilisateurs enregistrés
- `player` : informations et statistiques des joueurs
- `pokemon` : liste des Pokémon et leurs évolutions
- `type` : types de Pokémon et effets associés
- `skill` : attaques possibles
- `object` : objets achetables en jeu
- `log` : historique des combats

Les relations sont gérées par des **clés étrangères** pour assurer la cohérence des données.

---

## Technologies
- **ASP.NET MVC**
- **Entity Framework**
- **SQL Server**
- **C#**
- **Git/GitHub**
- **HTML/CSS/JS** pour les interfaces

---

## Installation rapide

### Prérequis
- .NET SDK 6+
- SQL Server
- Git

### Cloner le dépôt
```bash
git clone https://github.com/votre-utilisateur/arena-legendes.git
cd arena-legendes
```

### Configuration de la base
Modifiez `appsettings.json` pour votre connexion SQL Server :
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ArenaLegendesDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```
Puis exécutez :
```bash
dotnet ef database update
```

### Lancer l'application
```bash
dotnet run
```
L'API est disponible sur `http://localhost:5000`.

---

## Fonctionnalités principales
- Gestion des joueurs et de leurs Pokémon
- Combats au tour par tour
- Attribution des compétences et évolutions
- Économie avec achats d'objets
- Historique des batailles

---

## Fonctionnalités avancées (bonus)
- Classement des meilleurs joueurs
- IA pour les adversaires
- Animations et effets visuels
- Quêtes et événements aléatoires

---

## Contribuer
- Backend commun développé en équipe
- Frontends individuels
- Modélisation de la base et migrations partagées
Pour plus de détails, consultez [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Critères d'évaluation

| Critère | Points |
|---------|-------:|
| Respect du pattern MVC | 3 |
| Qualité du code | 3 |
| Expérience utilisateur (UX) | 2 |
| Utilisation d'ASP.NET | 5 |
| Fonctionnalités implémentées | 5 |
| Workflow Git | 2 |

---

## Licence
Ce projet est distribué sous licence **MIT**.

---
