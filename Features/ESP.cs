using System;
using System.Collections.Generic;

namespace AmongUsExternal.Features
{
    public class ESPFeature
    {
        private PlayerManager playerManager;

        public ESPFeature(PlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        public void RenderConsoleRadar()
        {
            try
            {
                List<PlayerDataModel> players = playerManager.GetAllPlayers();
                if (players.Count == 0)
                {
                    Console.WriteLine("[ESP] Aktif lobi veya oyuncu bulunamadi.");
                    return;
                }

                Console.Clear();
                Console.WriteLine("=================================================");
                Console.WriteLine("         AMONG US EXTERNAL ESP / RADAR           ");
                Console.WriteLine("=================================================");
                Console.WriteLine(string.Format("Toplam Aktif Oyuncu: {0}\n", players.Count));

                Console.WriteLine(string.Format("{0,-15} {1,-12} {2,-10} {3,-15}", "ISIM", "ROL", "DURUM", "KONUM (X, Y)"));
                Console.WriteLine(new string('-', 55));

                foreach (PlayerDataModel p in players)
                {
                    string role = p.IsImpostor ? "IMPOSTOR" : "CREWMATE";
                    string status = p.IsDead ? "OLU" : "CANLI";

                    if (p.IsImpostor)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (p.IsDead)
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine(string.Format("{0,-15} {1,-12} {2,-10} ({3,6:F1}, {4,6:F1})", 
                        p.Name, role, status, p.PositionX, p.PositionY));
                }

                Console.ResetColor();
                Console.WriteLine("=================================================");
            }
            catch (Exception ex)
            {
                Logger.LogError("Radar cizimi sirasinda hata olustu", ex);
            }
        }
    }
}
