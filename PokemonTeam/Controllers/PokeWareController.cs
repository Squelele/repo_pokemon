using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonTeam.Data;      // injecte PokemonDbContext
using PokemonTeam.Models;     // Pokemon
using PokemonTeam.Models.PokeWare;
using PokemonTeam.Services;    // Pokemon, PokeWareQuestion, PokeWareSession
using System.Security.Claims;
using System.Threading.Tasks;

namespace PokemonTeam.Controllers;
/// <summary>
/// This controller manages the PokéWare quiz game logic.
/// </summary>
/// <remarks>
/// Actions :
/// <list type="bullet">
///   <item><description>Selection de l’équipe Pokémon</description></item>
///   <item><description>Choix du mode de jeu</description></item>
///   <item><description>Enchaînement des questions</description></item>
///   <item><description>Utilisation d’objets</description></item>
///   <item><description>Fin de partie et affichage du score</description></item>
/// </list>
/// </remarks>
/// <author>Elerig</author>
public class PokeWareController : Controller
{
    private readonly PokemonDbContext _context;
    private readonly PokemonController _pokemonController;
    private readonly PokeApiService _pokeApi;
    private readonly Random _rng = new();

    public PokeWareController(PokemonDbContext context, PokeApiService pokeApi)
    {
        _context = context;
        _pokemonController = new PokemonController(context);
        _pokeApi = pokeApi;
    }

    // --------------------------------------------------------------------
    // 1. Choix des 6 Pokémon
    // --------------------------------------------------------------------
    public async Task<IActionResult> SelectTeam()
    {
        var player = await GetCurrentPlayer(includePokemons: true);

        List<Pokemon> pokemons;
        if (player == null)
        {
            // Fallback : afficher tous les Pokémon si aucun joueur connecté
            pokemons = await _context.Pokemons
                .Include(p => p.Types)
                .ToListAsync();
        }
        else
        {
            pokemons = player.Pokemons;
        }

        return View(pokemons);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmTeam(List<int> selectedPokemonIds)
    {
        if (selectedPokemonIds == null || selectedPokemonIds.Count != 6)
        {
            TempData["Error"] = "Tu dois choisir exactement 6 Pokémon.";
            return RedirectToAction(nameof(SelectTeam));
        }

        // Récupérer directement les Pokémon depuis la base sans passer par
        // PokemonController. L'appel direct au contrôleur retournait toujours
        // une ActionResult sans valeur, ce qui laissait la liste vide.
        var pokemons = await _context.Pokemons
            .Include(p => p.Types)
            .Where(p => selectedPokemonIds.Contains(p.Id))
            .ToListAsync();

        if (pokemons.Count != selectedPokemonIds.Count)
        {
            TempData["Error"] = "Une erreur est survenue lors de la sélection des Pokémon.";
            return RedirectToAction(nameof(SelectTeam));
        }

        var session = new PokeWareSession
        {
            Pokemons = pokemons,
            LivesLeft = 6
        };

        HttpContext.Session.SetObject("QuizSession", session);
        return RedirectToAction(nameof(SelectMode));
    }

    // --------------------------------------------------------------------
    // 2. Choix du mode de jeu
    // --------------------------------------------------------------------
    public IActionResult SelectMode() => View();   // options : 10 / 20 / 50 / Infini

    [HttpPost]
    public async Task<IActionResult> StartQuiz(int numberOfQuestions)
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null)
            return RedirectToAction(nameof(SelectTeam));

        if (session.Pokemons == null || session.Pokemons.Count == 0)
        {
            TempData["Error"] = "Aucune équipe sélectionnée.";
            return RedirectToAction(nameof(SelectTeam));
        }

        if (numberOfQuestions <= 0)
        {
            TempData["Error"] = "Le nombre de questions doit être positif.";
            return RedirectToAction(nameof(SelectMode));
        }

        session.Questions = await GenerateQuizAsync(session.Pokemons, numberOfQuestions);
        session.CurrentQuestionIndex = 0;
        HttpContext.Session.SetObject("QuizSession", session);

        return RedirectToAction(nameof(Question));
    }

    // --------------------------------------------------------------------
    // 3. Affichage et soumission d’une question
    // --------------------------------------------------------------------
    public IActionResult Question()
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null || session.IsOver)
            return RedirectToAction(nameof(Result));

        return View(session.CurrentQuestion);
    }

    [HttpPost]
    public IActionResult SubmitAnswer(string userAnswer)
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null)
            return RedirectToAction(nameof(SelectTeam));

        var current = session.CurrentQuestion;
        if (current is null)
            return RedirectToAction(nameof(Result));

        if (ValidateAnswer(current, userAnswer))
        {
            session.Score += 10;
            session.PokeDollarsEarned += 5;
        }
        else
        {
            session.LivesLeft--;
        }

        session.CurrentQuestionIndex++;
        HttpContext.Session.SetObject("QuizSession", session);

        return RedirectToAction(nameof(Question));
    }

    // --------------------------------------------------------------------
    // 4. Boutique d’objets
    // --------------------------------------------------------------------
    public async Task<IActionResult> Shop()
    {
        var player = await GetCurrentPlayer(includeItems: true);

        if (player == null) return RedirectToAction("SelectTeam");

        var objects = player.Items;
        return View(objects);
    }

    [HttpPost]
    public async Task<IActionResult> UseObject(int objectId)
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null)
            return RedirectToAction(nameof(SelectTeam));

        string effect = await UseObjectAsync(objectId, session);
        HttpContext.Session.SetObject("QuizSession", session);

        TempData["Message"] = $"Objet utilisé : {effect}";
        return RedirectToAction(nameof(Question));
    }

    // --------------------------------------------------------------------
    // 5. Résultat final
    // --------------------------------------------------------------------
    public IActionResult Result()
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null)
            return RedirectToAction(nameof(SelectTeam));

        int bonus = session.LivesLeft * 10;
        session.Score += bonus;

        ViewBag.FinalScore = session.Score;
        ViewBag.Bonus = bonus;
        ViewBag.PokeDollars = session.PokeDollarsEarned;
        return View();
    }

    // --------------------------------------------------------------------
    // Helpers privés
    // --------------------------------------------------------------------
    private async Task<Player?> GetCurrentPlayer(bool includePokemons = false, bool includeItems = false)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return null;

        IQueryable<Player> query = _context.Players.Include(p => p.UserAuth);
        if (includePokemons)
            query = query.Include(p => p.Pokemons).ThenInclude(p => p.Types);
        if (includeItems)
            query = query.Include(p => p.Items);

        return await query.FirstOrDefaultAsync(p => p.UserAuth.Email == email && p.Game == "pokeWare");
    }

    /// <summary>
    /// Construit une liste de questions aléatoires.
    /// </summary>
    private async Task<List<PokeWareQuestion>> GenerateQuizAsync(List<Pokemon> team, int count)
    {
        if (team == null || team.Count == 0 || count <= 0)
            return new List<PokeWareQuestion>();

        var quiz = new List<PokeWareQuestion>(count);
        var allTypes = new[] { "fire", "water", "grass", "electric", "ice", "fighting", "poison", "ground", "flying", "psychic", "bug", "rock", "ghost", "dragon", "dark", "steel", "fairy", "normal" };

        for (int i = 0; i < count; i++)
        {
            if (_rng.Next(2) == 0)
            {
                var poke = team[_rng.Next(team.Count)];
                string correct = poke.Types.Any()
                    ? poke.Types[_rng.Next(poke.Types.Count)].Name
                    : "normal";
                var choices = new HashSet<string> { correct };
                while (choices.Count < 4)
                {
                    var candidate = allTypes[_rng.Next(allTypes.Length)];
                    choices.Add(candidate);
                }

                quiz.Add(new PokeWareQuestion
                {
                    QuestionText = $"Quel est le type élémentaire de {poke.name} ?",
                    CorrectAnswer = correct,
                    Choices = choices.OrderBy(_ => _rng.Next()).ToList()
                });
            }
            else
            {
                var (name, image) = await _pokeApi.GetRandomPokemonAsync();
                quiz.Add(new PokeWareQuestion
                {
                    QuestionText = "Quel est ce Pokémon ?",
                    ImageUrl = image,
                    CorrectAnswer = name,
                    Choices = new List<string>()
                });
            }
        }
        return quiz;
    }

    /// <summary>
    /// Compare la réponse de l’utilisateur à la bonne réponse.
    /// </summary>
    private static bool ValidateAnswer(PokeWareQuestion q, string? userAnswer)
        => q is not null &&
           string.Equals(q.CorrectAnswer?.Trim(),
                         userAnswer?.Trim(),
                         StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Applique l’effet d’un objet et retourne sa description.
    /// </summary>
    private async Task<string> UseObjectAsync(int objectId, PokeWareSession session)
    {
        var player = await GetCurrentPlayer(includeItems: true);
        if (player == null) return "Joueur inconnu";

        var playerObj = player.Items.FirstOrDefault(po => po.Id == objectId);
        if (playerObj is null) return "Objet inconnu";

        switch (playerObj.Name)
        {
            case "Potion":
                session.LivesLeft = Math.Min(session.LivesLeft + 1, 6);
                break;
            case "Super Potion":
                session.Score += 10;
                break;
            case "Hyper Potion":
                session.Score += 20;
                break;
            case "Max Potion":
                session.LivesLeft = 6;
                break;
        }

        player.Items.Remove(playerObj);
        await _context.SaveChangesAsync();
        return playerObj.Name;
    }
}
