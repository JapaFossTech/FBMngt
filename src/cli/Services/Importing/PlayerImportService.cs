using FBMngt.Data;
using FBMngt.Models;

namespace FBMngt.Services.Importing;

public class PlayerImportService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerImportService(
                        IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public virtual async Task InsertPlayersAsync(
                                    List<string> playerNames)
    {
        foreach (var name in playerNames)
        {
            var player = new Player
            {
                PlayerName = name,
                Aka1 = null,
                Aka2 = null
            };

            await _playerRepository.InsertAsync(player);

            Console.WriteLine(
               $"Inserted new player into tblPlayer: {name}");
        }
    }
}
