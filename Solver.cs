using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class Solver
{
    public int Move(
        Dictionary<int, (int id, float x, float y, int[] conns)> nodes,
        (float x, float y) target,
        (float x, float y) player,
        int targetLocId,
        int playerLocId
    )
    {
        var crr = nodes[playerLocId];
        var minor = Math.Sqrt(Math.Pow(target.x - player.x, 2) + Math.Pow(target.y - player.y, 2));
        int bestway = 0;
        foreach(var n in crr.conns)
        {
            var neig = nodes[n];
            var dist = Math.Sqrt(Math.Pow(neig.x - target.x, 2) + Math.Pow(neig.y - target.y, 2));
            if(dist < minor) 
                minor = dist;
                bestway = n;
        }

        if(!nodes.TryGetValue(bestway, out var m)) return playerLocId;

        return bestway;
    }
}