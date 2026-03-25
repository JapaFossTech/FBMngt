using FBMngt.Models;

namespace FBMngt.Services.Reporting.PreDraftRanking;

public interface IPreDraftRankingMovementCalculator
{
    List<PreDraftMovementRow> CalculateMovement(
        List<FanProsPlayer> startPlayers,
        List<FanProsPlayer> targetPlayers);
}

public class PreDraftRankingMovementCalculator
    : IPreDraftRankingMovementCalculator
{
    public List<PreDraftMovementRow> CalculateMovement(
        List<FanProsPlayer> startPlayers,
        List<FanProsPlayer> targetPlayers)
    {
        // Prepare simulation list

        List<FanProsPlayer> simulationPlayerList = 
            startPlayers
                .Where(p => p.PlayerID.HasValue)
                .ToList();

        List<int> originalYahooOrder_PlayerID =
            simulationPlayerList
                .Select(p => p.PlayerID!.Value)
                .ToList();

        // Reduce the target list to only have the
        // original list

        targetPlayers = targetPlayers
            .Where(p =>
                p.PlayerID.HasValue &&
                originalYahooOrder_PlayerID.Contains(
                    p.PlayerID.Value))
            .ToList();

        if (simulationPlayerList.Count 
                            != targetPlayers.Count)
        {
            Console.WriteLine(
                "Warning: Player universe mismatch detected.");
        }

        var movementRows = new List<PreDraftMovementRow>();

        // Loop target playes to move them if necessary

        for (int targetIndex = 0;
             targetIndex < targetPlayers.Count;
             targetIndex++)
        {
            FanProsPlayer targetPlayer = 
                    targetPlayers[targetIndex];

            if (!targetPlayer.PlayerID.HasValue)
            {
                if (targetIndex <= 350)
                    Console.WriteLine("");
                continue;
            }

            int targetPlayerId = targetPlayer.PlayerID.Value;

            // Get player index from the simulation, from the start
            // list that is changing with previous moves
            int playerIndexFromSimulation =
                simulationPlayerList
                    .FindIndex(p =>
                        p.PlayerID == targetPlayerId);

            if (playerIndexFromSimulation == -1)
            {
                if (targetIndex <= 350)
                    Console.WriteLine($"Target player '{targetPlayer}' " +
                    $"(with target index: {targetIndex}) " +
                    $"not found in original start list");
                continue; // not in original start list
            }

            // IMPORTANT:
            // Clamp targetIndex to valid range
            int safeTargetIndex =
                Math.Min(targetIndex,
                         simulationPlayerList.Count - 1);

            int movement = playerIndexFromSimulation
                            - safeTargetIndex;

            if (movement != 0)
            {
                FanProsPlayer playerToMove =
                    simulationPlayerList[playerIndexFromSimulation];

                // Target position removed from working list,
                // All below players move up 1
                simulationPlayerList
                    .RemoveAt(playerIndexFromSimulation);

                // Because we removed 1, when moving down, make
                // sure the target index is within the simulation
                if (playerIndexFromSimulation < safeTargetIndex)
                    safeTargetIndex--;

                // Target position inserted into working list using
                // his safeTargetIndex.Players move down 1 poistion.
                simulationPlayerList
                    .Insert(safeTargetIndex, playerToMove);
            }

            movementRows.Add(new PreDraftMovementRow
            {
                PlayerID = targetPlayerId,
                PlayerName = targetPlayer.PlayerName!,
                CurrentRank = playerIndexFromSimulation + 1,
                TargetRank = safeTargetIndex + 1,
                Movement = movement
            });
        }

        return movementRows;
    }
}