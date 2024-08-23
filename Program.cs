using Pamella;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text.Json;
using System.Collections.Generic;

if (args.Contains("gen"))
    KmlPathsToJson();

var main = new Main(GetLocs());
App.Open(main);

List<Location> GetLocs()
{
    var json = File.ReadAllText("bosch.json");
    return JsonSerializer.Deserialize<List<Location>>(json) ?? [];
}

void KmlPathsToJson()
{
    var lines = File.ReadAllLines("Sem título.kml");

    List<Location> locations = [];

    bool getNext = false;
    int id = 1;
    foreach (var line in lines)
    {
        if (line.Contains("<coordinates>"))
        {
            getNext = true;
            continue;
        }

        if (!getNext)
            continue;
        getNext = false;

        var dots = line.Trim().Split(' ');
        Location last = null;
        for (int i = 0; i < dots.Length; i++)
        {
            var dot = dots[i];
            var data = dot.Split(',');
            var newLoc = new Location(
                id++,
                double.Parse(data[0].Replace('.', ',')),
                double.Parse(data[1].Replace('.', ',')),
                last is null ? [] : [ last.Id ]
            );
            locations.Add(newLoc);
            last?.Connections?.Add(newLoc.Id);
            last = newLoc;
        }
    }

    Dictionary<int, int> removeMap = [];
    for (int i = 0; i < locations.Count; i++)
    {
        var loci = locations[i];
        for (int j = 0; j < locations.Count; j++)
        {
            if (i == j)
                continue;
            
            var locj = locations[j];

            if (loci.Connections.Contains(locj.Id))
                continue;

            var dx = loci.Latitude - locj.Latitude;
            var dy = loci.Longitude - locj.Longitude;
            var dist = dx * dx + dy * dy;
            
            if (dist > 1e-9)
                continue;
            if (!removeMap.ContainsKey(j))
                removeMap.Add(j, i);

            locations.Remove(locj);
            j--;
            foreach (var conn in locj.Connections)
            {
                if (loci.Connections.Contains(conn))
                    continue;
                
                loci.Connections.Add(conn);
                var obj = locations.FirstOrDefault(l => l.Id == conn);
                if (obj is null)
                    continue;
                
                if (obj.Connections.Contains(loci.Id))
                    continue;
                
                obj.Connections.Add(loci.Id);
            }
        }
    }

    var opts = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText("bosch.json", JsonSerializer.Serialize(locations, opts));
}

public record Location(
    int Id,
    double Latitude,
    double Longitude,
    List<int> Connections
);

public class Main(List<Location> locations) : View
{
    Location player = null;
    Location nextPlayer = null;
    float playerX = 0, playerY = 0;
    float playerTgX = 0, playerTgY = 0;

    Location ademir = null;
    Location nextAdemir = null;
    float ademirX = 0, ademirY = 0;
    float ademirTgX = 0, ademirTgY = 0;

    void Reset()
    {
        ademir = locations
            .OrderBy(x => Random.Shared.Next())
            .FirstOrDefault();
        (ademirX, ademirY) = toScreen(ademir, 0, 0);
        nextAdemir = null;
        discoverNextAdemir();

        nextPlayer = locations
            .OrderBy(x => Random.Shared.Next())
            .FirstOrDefault();
        (playerX, playerY) = toScreen(nextPlayer, 0, 0);
        discoverNextPlayer();
    }

    Image map = Image.FromFile("Bosch.png");
    Image ademirPng = Image.FromFile("ademir.png");
    Image playerPng = Image.FromFile("player.png");
    float vx = 0;
    float vy = 0;
    float scale = 0f;
    readonly float moveStep = 25;
    readonly float adjust = 39.7f;
    readonly float adjust2 = 33.4f;
    readonly float theta = -1.5408f;
    readonly float dx = 1720;
    readonly float dy = -180;

    protected override void OnStart(IGraphics g)
    {
        Reset();
        AlwaysInvalidateMode();
        g.SubscribeKeyDownEvent(key =>
        {
            if (key == Input.Escape)
                App.Close();
        
            switch (key)
            {
                case Input.D:
                    vx -= moveStep;
                    break;
                    
                case Input.A:
                    vx += moveStep;
                    break;
                
                case Input.W:
                    vy += moveStep;
                    break;
                    
                case Input.S:
                    vy -= moveStep;
                    break;
                
                case Input.E:
                    scale += 0.1f;
                    if (scale > 1.5f)
                        scale = 1.5f;
                    break;

                case Input.Q:
                    scale -= 0.1f;
                    if (scale < 0.5f)
                        scale = 0.5f;
                    break;
                
                // case Input.Z:
                //     adjust += 0.1f;
                //     break;
                
                // case Input.X:
                //     adjust -= 0.1f;
                //     break;
                
                // case Input.C:
                //     adjust2 += 0.1f;
                //     break;
                
                // case Input.V:
                //     adjust2 -= 0.1f;
                //     break;
                
                // case Input.R:
                //     theta += 0.01f;
                //     break;
                
                // case Input.T:
                //     theta -= 0.01f;
                //     break;
                
                // case Input.Right:
                //     dx += 10f;
                //     break;
                
                // case Input.Left:
                //     dx -= 10f;
                //     break;
                
                // case Input.Down:
                //     dy += 10f;
                //     break;
                
                // case Input.Up:
                //     dy -= 10f;
                //     break;
                
                // case Input.Space:
                //     System.Windows.Forms.MessageBox.Show($"{adjust} {adjust2} {dx} {dy} {theta}");
                //     break;
            }
        });
    }

    DateTime dt = DateTime.Now;
    protected override void OnFrame(IGraphics g)
    {
        if (map.Width * scale < g.Width)
            scale = g.Width / (float)map.Width;
        
        if (map.Height * scale < g.Height)
            scale = g.Height / (float)map.Height;
        
        if (vx * scale > 0)
            vx = 0;
        
        if (vy * scale > 0)
            vy = 0;
        
        if ((vx + map.Width) * scale < g.Width)
            vx = g.Width / scale - map.Width;
        
        if ((vy + map.Height) * scale < g.Height)
            vy = g.Height / scale - map.Height;
        
        var newDt = DateTime.Now;
        var time = (float)(newDt - dt).TotalSeconds;
        dt = newDt;
        moveAdemir();
        movePlayer();

        var dx = ademirX - playerX;
        var dy = ademirY - playerY;
        var mod = MathF.Sqrt(dx * dx + dy * dy);
        if (mod < 10)
            Reset();

        void moveAdemir()
        {
            var dx = ademirTgX - ademirX;
            var dy = ademirTgY - ademirY;
            var mod = MathF.Sqrt(dx * dx + dy * dy);
            if (mod < 4)
            {
                discoverNextAdemir();
                return;
            }

            dx /= mod;
            dy /= mod;
        
            ademirX += dx * time * 40;
            ademirY += dy * time * 40;
        }

        void movePlayer()
        {
            var dx = playerTgX - playerX;
            var dy = playerTgY - playerY;
            var mod = MathF.Sqrt(dx * dx + dy * dy);
            if (mod < 4)
            {
                discoverNextPlayer();
                return;
            }

            dx /= mod;
            dy /= mod;
        
            playerX += dx * time * 60;
            playerY += dy * time * 60;
        }
    }

    protected override void OnRender(IGraphics g)
    {
        var view = new RectangleF(
            vx * scale, vy * scale,
            map.Width * scale, map.Height * scale
        );
        g.DrawImage(view, map);

        g.DrawImage(new RectangleF(ademirX + vx - 15, ademirY + vy - 15, 30, 30), ademirPng);
        g.DrawImage(new RectangleF(playerX + vx - 15, playerY + vy - 15, 30, 30), playerPng);
    }

    (float x, float y) toScreen(Location location, float vx, float vy)
    {
        const double lat0 = -49.32233967173136;
        const double lon0 = -25.53095171972581;
        var x = location.Longitude - lon0;
        var y = location.Latitude - lat0;
        x *= -10_000 * adjust;
        y *= -10_000 * adjust2;
        var rx = x * MathF.Cos(theta) + y * MathF.Sin(theta) + dx + vx;
        var ry = y * MathF.Cos(theta) - x * MathF.Sin(theta) + dy + vy;

        return ((float)rx, (float)ry);
    }

    void discoverNextPlayer()
    {
        var solver = new Solver();
        var dict = locations
            .Select(lc => {
                (float x, float y) = toScreen(lc, 0, 0);
                return (lc.Id, x, y, lc.Connections.ToArray());
            })
            .ToDictionary(t => t.Id);
        player = nextPlayer;
        var next = solver.Move(dict,
            (ademirX, ademirY),
            (playerX, playerY),
            ademir.Id, player.Id
        );
        nextPlayer = locations
            .FirstOrDefault(l => l.Id == next);
        (playerTgX, playerTgY) = toScreen(nextPlayer, 0, 0);
    }

    void discoverNextAdemir()
    {
        if (nextAdemir is not null)
            ademir = nextAdemir;
        nextAdemir = null;
        while (nextAdemir is null)
        {
            var ways = 
                from conn in ademir.Connections
                orderby Random.Shared.Next()
                select conn;
            var next = ways.FirstOrDefault();
            nextAdemir = locations.FirstOrDefault(loc => loc.Id == next);
        }
        (ademirTgX, ademirTgY) = toScreen(nextAdemir, 0, 0);
    }

    void drawMap(IGraphics g)
    {
        foreach (var loc in locations)
        {
            var (x, y) = toScreen(loc, vx, vy);
            
            g.DrawText(new RectangleF(x, y - 15, 40, 15), loc.Id.ToString());
            g.FillRectangle(
                x, y, 5, 5, 
                Brushes.Orange
            );

            foreach (var conn in loc.Connections)
            {
                var other = locations.FirstOrDefault(l => l.Id == conn);
                if (other is null)
                    continue;
                
                var (x2, y2) = toScreen(other, vx, vy);
                
                g.DrawPolygon([
                    new PointF((float)x, (float)y),
                    new PointF((float)x2, (float)y2),
                    new PointF((float)x, (float)y)
                ], Pens.Orange);
            }
        }
    }
}