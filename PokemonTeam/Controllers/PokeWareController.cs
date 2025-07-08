using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonTeam.Data;      // injecte PokemonDbContext
using PokemonTeam.Models.PokeWare;    // Pokemon, PokeWareQuestion, PokeWareSession
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Random _rng = new();

    public PokeWareController(PokemonDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // --------------------------------------------------------------------
    // 1. Choix des 6 Pokémon
    // --------------------------------------------------------------------
    public async Task<IActionResult> SelectTeam()
    {
        int playerId = GetPlayerId();
        // Récupère d’abord le Player puis sa collection de Pokémons
        var player = await _context.Players
            .Include(p => p.Pokemons)
                .ThenInclude(pok => pok.Types)
            .FirstOrDefaultAsync(p => p.Id == GetPlayerId());

        if (player == null) return RedirectToAction("Index", "Home");

        var pokemons = player.Pokemons;
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

        var session = new PokeWareSession
        {
            Pokemons = await _context.Pokemons
                                     .Include(p => p.Types)
                                     .Where(p => selectedPokemonIds.Contains(p.Id))
                                     .ToListAsync(),
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
    public IActionResult StartQuiz(int numberOfQuestions)
    {
        var session = HttpContext.Session.GetObject<PokeWareSession>("QuizSession");
        if (session is null)
            return RedirectToAction(nameof(SelectTeam));

        session.Questions = GenerateQuiz(session.Pokemons, numberOfQuestions);
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
        int playerId = GetPlayerId();
        var player = await _context.Players
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == GetPlayerId());

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
    private int GetPlayerId()
        => int.Parse(User.Claims.First(c => c.Type == "PlayerId").Value);

    /// <summary>
    /// Construit une liste de questions à partir de la team.
    /// </summary>
    private List<PokeWareQuestion> GenerateQuiz(List<Pokemon> team, int count)
    {
        var quiz = new List<PokeWareQuestion>(count);
        for (int i = 0; i < count; i++)
        {
            var poke = team[_rng.Next(team.Count)];
            string pokeType = poke.Types.Any()
                ? poke.Types[_rng.Next(poke.Types.Count)].Name
                : "Inconnu";
            quiz.Add(new PokeWareQuestion
            {
                QuestionText = $"Quel est le type élémentaire de {poke.name} ?",
                CorrectAnswer = pokeType,
                Choices = new List<string> { pokeType }
            });
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
        var playerObj = await _context.Items
                                      .FirstOrDefaultAsync(po => po.Id == objectId);
        if (playerObj is null) return "Objet inconnu";

        switch (playerObj.Name)
        {
            case "REVIVE":
                session.LivesLeft = Math.Min(session.LivesLeft + 1, 6);
                break;
            case "DOUBLE_XP":
                session.Score += 10;
                break;
                // autres effets...
        }

        _context.Items.Remove(playerObj);
        await _context.SaveChangesAsync();
        return playerObj.Name;
    }
}
