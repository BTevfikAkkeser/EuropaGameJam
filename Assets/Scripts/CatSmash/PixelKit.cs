using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct Stamp
{
    public Texture2D Tex;
    public Color32 A, B, C;

    public Stamp(Texture2D tex, Color32 a, Color32 b, Color32 c)
    {
        Tex = tex;
        A = a;
        B = b;
        C = c;
    }
}

public class Pix
{
    public readonly int W;
    public readonly int H;
    readonly Color32[] px;

    public Pix(int w, int h)
    {
        W = w;
        H = h;
        px = new Color32[w * h];
    }

    public static Color32 C(int r, int g, int b, int a = 255)
    {
        return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
    }

    public void P(int x, int y, Color32 c)
    {
        if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
        px[y * W + x] = c;
    }

    public void Rect(int x, int y, int w, int h, Color32 c)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                P(xx, yy, c);
    }

    public void Ellipse(int cx, int cy, int rx, int ry, Color32 c)
    {
        if (rx < 1) rx = 1;
        if (ry < 1) ry = 1;
        int rx2 = rx * rx;
        int ry2 = ry * ry;
        int gate = rx2 * ry2;
        for (int y = -ry; y <= ry; y++)
            for (int x = -rx; x <= rx; x++)
                if (x * x * ry2 + y * y * rx2 <= gate)
                    P(cx + x, cy + y, c);
    }

    public void Outline(Color32 ink)
    {
        var src = (Color32[])px.Clone();
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (src[y * W + x].a != 0) continue;
                if (Filled(src, x + 1, y) || Filled(src, x - 1, y) || Filled(src, x, y + 1) || Filled(src, x, y - 1))
                    px[y * W + x] = ink;
            }
        }
    }

    bool Filled(Color32[] src, int x, int y)
    {
        return (uint)x < (uint)W && (uint)y < (uint)H && src[y * W + x].a != 0;
    }

    public Texture2D ToTex(string name)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return tex;
    }
}

public static class PixelKit
{
    static readonly Dictionary<string, Stamp> CacheMap = new Dictionary<string, Stamp>();
    static Shader shader;
    static readonly Color32 Ink = Pix.C(28, 18, 22);

    public static Material Mat(Texture2D tex, bool repeat = false)
    {
        if (shader == null) shader = Shader.Find("Cat/Pixel");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.mainTexture = tex;
        if (repeat)
        {
            tex.wrapMode = TextureWrapMode.Repeat;
            mat.SetTextureScale("_BaseMap", new Vector2(11f, 8f));
        }
        return mat;
    }

    public static GameObject Make(string name, Texture2D tex, float worldWidth)
    {
        var root = new GameObject(name);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        float aspect = tex.height / (float)tex.width;
        root.transform.localScale = new Vector3(worldWidth, worldWidth * aspect, 1f);
        var renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Mat(tex);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        visual.AddComponent<DepthSort>();
        visual.AddComponent<OwnedMat>();
        return root;
    }

    public static GameObject Quad(string name, Material mat, Vector2 pos, Vector2 size, float z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = new Vector3(pos.x, pos.y, z);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(mat);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.AddComponent<OwnedMat>();
        return go;
    }

    static Stamp Cache(string key, Func<Stamp> build)
    {
        if (CacheMap.TryGetValue(key, out var stamp)) return stamp;
        stamp = build();
        CacheMap[key] = stamp;
        return stamp;
    }

    public static Texture2D Floor()
    {
        var pix = new Pix(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool seam = y % 8 == 0 || (((y / 8) % 2 == 0) ? x == 0 : x == 16);
                int n = ((x * 17 + y * 31) ^ (y * 13)) & 15;
                Color32 c = seam
                    ? Pix.C(78, 54, 36)
                    : (n > 10 ? Pix.C(128, 90, 58) : Pix.C(112, 78, 50));
                pix.P(x, y, c);
            }
        }
        var tex = pix.ToTex("floor");
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    public static Texture2D Wall()
    {
        var pix = new Pix(16, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int n = (x * 19 + y * 7) & 7;
                pix.P(x, y, n == 0 ? Pix.C(62, 50, 44) : Pix.C(46, 38, 34));
            }
        }
        var tex = pix.ToTex("wall");
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    public static Stamp Cat(bool happy)
    {
        return Cache(happy ? "cat-happy" : "cat", () => BuildCat(happy));
    }

    static Stamp BuildCat(bool happy)
    {
        var pix = new Pix(32, 32);
        var fur = Pix.C(224, 126, 58);
        var dark = Pix.C(176, 78, 32);
        var light = Pix.C(244, 176, 104);
        var cream = Pix.C(246, 236, 220);
        var pink = Pix.C(232, 140, 156);
        pix.Ellipse(16, 12, 8, 6, fur);
        pix.Ellipse(16, 12, 5, 4, light);
        pix.Rect(11, 10, 2, 5, dark);
        pix.Rect(15, 9, 2, 6, dark);
        pix.Rect(19, 10, 2, 5, dark);
        pix.Ellipse(10, 8, 2, 2, cream);
        pix.Ellipse(22, 8, 2, 2, cream);
        pix.Ellipse(16, 6, 2, 3, dark);
        pix.Ellipse(21, 4, 3, 2, dark);
        pix.Ellipse(11, 27, 3, 3, fur);
        pix.Ellipse(21, 27, 3, 3, fur);
        pix.Ellipse(11, 27, 1, 2, pink);
        pix.Ellipse(21, 27, 1, 2, pink);
        pix.Ellipse(16, 22, 6, 5, fur);
        pix.Ellipse(16, 21, 4, 3, cream);
        if (happy)
        {
            pix.Rect(12, 23, 3, 1, Ink);
            pix.Rect(18, 23, 3, 1, Ink);
        }
        else
        {
            pix.Ellipse(13, 23, 1, 1, cream);
            pix.Ellipse(19, 23, 1, 1, cream);
            pix.P(13, 23, Ink);
            pix.P(19, 23, Ink);
        }
        pix.P(16, 21, Pix.C(214, 96, 112));
        pix.Ellipse(16, 16, 2, 1, Pix.C(196, 48, 58));
        pix.P(16, 16, Pix.C(236, 196, 72));
        pix.Outline(Ink);
        pix.P(8, 21, Ink);
        pix.P(7, 22, Ink);
        pix.P(24, 21, Ink);
        pix.P(25, 22, Ink);
        return new Stamp(pix.ToTex(happy ? "cat-happy" : "cat"), fur, dark, cream);
    }

    public static Stamp Human(bool smile)
    {
        return Cache(smile ? "human-smile" : "human", () =>
        {
            var pix = new Pix(24, 32);
            var skin = Pix.C(232, 184, 150);
            var hair = Pix.C(42, 32, 28);
            var shirt = Pix.C(58, 108, 156);
            var pants = Pix.C(46, 52, 70);
            pix.Ellipse(12, 24, 5, 4, hair);
            pix.Ellipse(12, 22, 4, 4, skin);
            pix.Ellipse(12, 14, 6, 5, shirt);
            pix.Ellipse(6, 15, 2, 3, shirt);
            pix.Ellipse(18, 15, 2, 3, shirt);
            pix.Ellipse(6, 12, 1, 1, skin);
            pix.Ellipse(18, 12, 1, 1, skin);
            pix.Ellipse(9, 7, 3, 3, pants);
            pix.Ellipse(15, 7, 3, 3, pants);
            pix.P(10, 23, Ink);
            pix.P(14, 23, Ink);
            if (smile) pix.Rect(11, 20, 3, 1, Pix.C(176, 92, 96));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex(smile ? "human-smile" : "human"), skin, shirt, hair);
        });
    }

    public static Stamp Hand()
    {
        return Cache("hand", () =>
        {
            var pix = new Pix(8, 8);
            var skin = Pix.C(232, 184, 150);
            pix.Ellipse(4, 3, 2, 2, skin);
            pix.Rect(3, 1, 2, 2, skin);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("hand"), skin, skin, skin);
        });
    }

    public static Stamp Couch()
    {
        return Cache("couch", () =>
        {
            var pix = new Pix(64, 36);
            var cloth = Pix.C(122, 52, 68);
            var dark = Pix.C(78, 30, 44);
            var wood = Pix.C(92, 58, 34);
            pix.Rect(4, 16, 56, 14, dark);
            pix.Rect(6, 6, 52, 16, cloth);
            pix.Rect(8, 8, 14, 12, Pix.C(146, 70, 86));
            pix.Rect(24, 8, 16, 12, Pix.C(146, 70, 86));
            pix.Rect(42, 8, 14, 12, Pix.C(146, 70, 86));
            pix.Rect(2, 8, 6, 20, dark);
            pix.Rect(56, 8, 6, 20, dark);
            pix.Rect(10, 2, 4, 4, wood);
            pix.Rect(50, 2, 4, 4, wood);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("couch"), cloth, dark, wood);
        });
    }

    public static Stamp SideTable()
    {
        return Cache("side", () =>
        {
            var pix = new Pix(18, 18);
            var wood = Pix.C(168, 112, 64);
            var dark = Pix.C(96, 60, 32);
            pix.Rect(2, 4, 14, 12, wood);
            pix.Rect(3, 2, 2, 3, dark);
            pix.Rect(13, 2, 2, 3, dark);
            pix.Rect(3, 14, 2, 2, dark);
            pix.Rect(13, 14, 2, 2, dark);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("side"), wood, dark, wood);
        });
    }

    public static Stamp Rug()
    {
        return Cache("rug", () =>
        {
            var pix = new Pix(48, 32);
            var red = Pix.C(148, 54, 48);
            var edge = Pix.C(92, 32, 36);
            var gold = Pix.C(196, 154, 78);
            pix.Ellipse(24, 16, 20, 12, edge);
            pix.Ellipse(24, 16, 17, 9, red);
            pix.Ellipse(24, 16, 8, 4, gold);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("rug"), red, gold, edge);
        });
    }

    public static Stamp Picture()
    {
        return Cache("picture", () =>
        {
            var pix = new Pix(20, 16);
            var frame = Pix.C(120, 78, 42);
            pix.Rect(1, 1, 18, 14, frame);
            pix.Rect(3, 3, 14, 10, Pix.C(92, 140, 168));
            pix.Ellipse(10, 7, 3, 2, Pix.C(224, 126, 58));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("picture"), frame, frame, frame);
        });
    }

    public static Stamp Yarn()
    {
        return Cache("yarn", () =>
        {
            var pix = new Pix(16, 16);
            var yarn = Pix.C(196, 64, 72);
            var dark = Pix.C(140, 36, 48);
            pix.Ellipse(8, 8, 5, 5, yarn);
            pix.P(5, 8, dark); pix.P(6, 10, dark); pix.P(8, 11, dark);
            pix.P(10, 10, dark); pix.P(11, 8, dark); pix.P(10, 6, dark);
            pix.P(8, 5, dark); pix.P(6, 6, dark);
            pix.Outline(Ink);
            pix.P(13, 7, yarn); pix.P(14, 6, yarn);
            return new Stamp(pix.ToTex("yarn"), yarn, dark, yarn);
        });
    }

    public static Stamp Mug()
    {
        return Cache("mug", () =>
        {
            var cup = Pix.C(236, 236, 232);
            var drink = Pix.C(92, 58, 36);
            var pix = new Pix(16, 16);
            pix.Ellipse(7, 8, 4, 4, cup);
            pix.Ellipse(7, 9, 2, 2, drink);
            pix.Rect(11, 7, 2, 3, cup);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("mug"), cup, drink, Pix.C(180, 180, 176));
        });
    }

    public static Stamp Book()
    {
        return Cache("book", () =>
        {
            var cover = Pix.C(58, 92, 148);
            var page = Pix.C(236, 226, 200);
            var pix = new Pix(16, 14);
            pix.Rect(2, 2, 12, 10, cover);
            pix.Rect(4, 3, 8, 8, page);
            pix.Rect(3, 3, 2, 8, Pix.C(36, 64, 112));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("book"), cover, page, cover);
        });
    }

    public static Stamp Shoe()
    {
        return Cache("shoe", () =>
        {
            var leather = Pix.C(72, 48, 36);
            var sole = Pix.C(46, 36, 30);
            var pix = new Pix(18, 12);
            pix.Ellipse(9, 5, 6, 3, leather);
            pix.Rect(3, 3, 10, 2, sole);
            pix.Rect(12, 6, 3, 2, leather);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("shoe"), leather, sole, leather);
        });
    }

    public static Stamp Remote()
    {
        return Cache("remote", () =>
        {
            var body = Pix.C(48, 48, 52);
            var pix = new Pix(8, 16);
            pix.Rect(2, 2, 4, 12, body);
            pix.P(3, 11, Pix.C(180, 48, 48));
            pix.P(5, 11, Pix.C(48, 48, 48));
            pix.P(3, 9, Pix.C(180, 180, 180));
            pix.P(5, 9, Pix.C(180, 180, 180));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("remote"), body, body, body);
        });
    }

    public static Stamp Bowl()
    {
        return Cache("bowl", () =>
        {
            var bowl = Pix.C(214, 206, 188);
            var food = Pix.C(196, 140, 64);
            var pix = new Pix(16, 14);
            pix.Ellipse(8, 6, 5, 3, bowl);
            pix.Ellipse(8, 7, 3, 2, food);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("bowl"), bowl, food, bowl);
        });
    }

    public static Stamp Plant()
    {
        return Cache("plant", () =>
        {
            var leaf = Pix.C(62, 140, 64);
            var leafDark = Pix.C(36, 96, 42);
            var pot = Pix.C(176, 86, 58);
            var pix = new Pix(20, 24);
            pix.Ellipse(10, 15, 5, 4, leafDark);
            pix.Ellipse(6, 16, 3, 3, leaf);
            pix.Ellipse(14, 16, 3, 3, leaf);
            pix.Ellipse(10, 19, 3, 3, leaf);
            pix.Rect(6, 3, 8, 7, pot);
            pix.Rect(5, 8, 10, 2, Pix.C(140, 64, 42));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("plant"), leaf, pot, leafDark);
        });
    }

    public static Stamp Vase()
    {
        return Cache("vase", () =>
        {
            var vase = Pix.C(78, 112, 156);
            var bloom = Pix.C(220, 86, 96);
            var pix = new Pix(14, 20);
            pix.Ellipse(7, 6, 3, 4, vase);
            pix.Ellipse(7, 11, 2, 2, vase);
            pix.Ellipse(5, 15, 2, 2, bloom);
            pix.Ellipse(9, 16, 2, 2, Pix.C(236, 196, 86));
            pix.Rect(7, 12, 1, 3, Pix.C(40, 110, 48));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("vase"), vase, bloom, vase);
        });
    }

    public static Stamp Lamp()
    {
        return Cache("lamp", () =>
        {
            var shade = Pix.C(236, 206, 120);
            var stand = Pix.C(92, 78, 64);
            var pix = new Pix(16, 20);
            pix.Ellipse(8, 13, 5, 3, shade);
            pix.Rect(7, 4, 2, 8, stand);
            pix.Ellipse(8, 4, 3, 1, stand);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("lamp"), shade, stand, shade);
        });
    }

    public static Stamp Chair()
    {
        return Cache("chair", () =>
        {
            var wood = Pix.C(164, 106, 60);
            var dark = Pix.C(96, 60, 32);
            var seat = Pix.C(142, 70, 78);
            var pix = new Pix(20, 24);
            pix.Rect(4, 14, 12, 5, dark);
            pix.Rect(4, 7, 12, 9, seat);
            pix.Rect(4, 4, 2, 4, wood);
            pix.Rect(14, 4, 2, 4, wood);
            pix.Rect(4, 16, 2, 3, wood);
            pix.Rect(14, 16, 2, 3, wood);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("chair"), wood, seat, dark);
        });
    }

    public static Stamp Speaker()
    {
        return Cache("speaker", () =>
        {
            var body = Pix.C(42, 40, 44);
            var cone = Pix.C(70, 68, 74);
            var pix = new Pix(14, 18);
            pix.Rect(2, 2, 10, 14, body);
            pix.Ellipse(7, 11, 3, 3, cone);
            pix.Ellipse(7, 6, 2, 2, cone);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("speaker"), body, cone, body);
        });
    }

    public static Stamp Crate()
    {
        return Cache("crate", () =>
        {
            var wood = Pix.C(176, 122, 68);
            var dark = Pix.C(110, 70, 36);
            var pix = new Pix(18, 18);
            pix.Rect(2, 2, 14, 14, wood);
            pix.Rect(3, 3, 12, 2, dark);
            pix.Rect(3, 8, 12, 2, dark);
            pix.Rect(3, 13, 12, 2, dark);
            pix.Rect(8, 3, 2, 12, dark);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("crate"), wood, dark, wood);
        });
    }

    public static Stamp Monitor()
    {
        return Cache("monitor", () =>
        {
            var bezel = Pix.C(36, 38, 44);
            var screen = Pix.C(92, 168, 186);
            var pix = new Pix(24, 20);
            pix.Rect(2, 6, 20, 12, bezel);
            pix.Rect(4, 8, 16, 8, screen);
            pix.Rect(6, 10, 6, 4, Pix.C(236, 236, 232));
            pix.Rect(11, 3, 2, 3, bezel);
            pix.Rect(8, 2, 8, 2, bezel);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("monitor"), bezel, screen, bezel);
        });
    }

    public static Stamp Trash()
    {
        return Cache("trash", () =>
        {
            var bin = Pix.C(88, 96, 104);
            var pix = new Pix(14, 16);
            pix.Rect(3, 2, 8, 10, bin);
            pix.Rect(2, 11, 10, 2, Pix.C(64, 70, 78));
            pix.Rect(6, 5, 2, 4, Pix.C(210, 210, 214));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("trash"), bin, bin, bin);
        });
    }

    public static Stamp Shelf()
    {
        return Cache("shelf", () =>
        {
            var wood = Pix.C(120, 74, 42);
            var pix = new Pix(28, 36);
            pix.Rect(2, 2, 24, 32, wood);
            Color32[] books = { Pix.C(176, 64, 58), Pix.C(58, 96, 160), Pix.C(214, 176, 72), Pix.C(62, 140, 84), Pix.C(140, 72, 150) };
            for (int row = 0; row < 3; row++)
            {
                int y = 5 + row * 10;
                pix.Rect(3, y + 7, 22, 2, Pix.C(78, 48, 28));
                for (int i = 0; i < 5; i++)
                    pix.Rect(4 + i * 4, y, 3, 7, books[(i + row) % books.Length]);
            }
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("shelf"), wood, books[0], books[1]);
        });
    }

    public static Stamp Tv()
    {
        return Cache("tv", () =>
        {
            var bezel = Pix.C(28, 28, 32);
            var pix = new Pix(36, 24);
            pix.Rect(2, 6, 32, 16, bezel);
            pix.Rect(4, 8, 28, 12, Pix.C(64, 120, 150));
            pix.Rect(8, 10, 8, 6, Pix.C(220, 180, 90));
            pix.Rect(18, 11, 8, 4, Pix.C(240, 240, 236));
            pix.Rect(16, 3, 4, 3, bezel);
            pix.Rect(10, 2, 16, 2, Pix.C(48, 48, 52));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("tv"), bezel, Pix.C(64, 120, 150), bezel);
        });
    }

    public static Stamp Table()
    {
        return Cache("table", () =>
        {
            var wood = Pix.C(186, 132, 74);
            var dark = Pix.C(110, 70, 36);
            var pix = new Pix(36, 24);
            pix.Rect(3, 5, 30, 14, wood);
            pix.Rect(5, 7, 26, 10, Pix.C(204, 156, 96));
            pix.Rect(5, 4, 3, 3, dark);
            pix.Rect(28, 4, 3, 3, dark);
            pix.Rect(5, 17, 3, 3, dark);
            pix.Rect(28, 17, 3, 3, dark);
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("table"), wood, dark, wood);
        });
    }

    public static Stamp Desk()
    {
        return Cache("desk", () =>
        {
            var wood = Pix.C(150, 104, 64);
            var pix = new Pix(40, 26);
            pix.Rect(2, 4, 36, 18, wood);
            pix.Rect(6, 14, 14, 4, Pix.C(210, 206, 196));
            pix.Rect(8, 8, 16, 3, Pix.C(40, 40, 46));
            pix.Rect(24, 12, 8, 6, Pix.C(230, 230, 226));
            pix.Rect(4, 3, 2, 3, Pix.C(90, 58, 32));
            pix.Rect(34, 3, 2, 3, Pix.C(90, 58, 32));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("desk"), wood, Pix.C(40, 40, 46), wood);
        });
    }

    public static Stamp Fridge()
    {
        return Cache("fridge", () =>
        {
            var body = Pix.C(226, 230, 232);
            var dark = Pix.C(170, 178, 184);
            var pix = new Pix(20, 36);
            pix.Rect(2, 2, 16, 32, body);
            pix.Rect(3, 18, 14, 1, dark);
            pix.Rect(14, 24, 1, 6, Pix.C(120, 128, 136));
            pix.Rect(14, 10, 1, 4, Pix.C(120, 128, 136));
            pix.Rect(5, 28, 3, 3, Pix.C(180, 60, 58));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("fridge"), body, dark, body);
        });
    }

    public static Stamp Wardrobe()
    {
        return Cache("wardrobe", () =>
        {
            var wood = Pix.C(112, 70, 42);
            var door = Pix.C(146, 96, 58);
            var pix = new Pix(28, 40);
            pix.Rect(2, 2, 24, 36, wood);
            pix.Rect(4, 4, 9, 30, door);
            pix.Rect(15, 4, 9, 30, door);
            pix.Rect(12, 16, 1, 4, Pix.C(210, 186, 120));
            pix.Rect(15, 16, 1, 4, Pix.C(210, 186, 120));
            pix.Outline(Ink);
            return new Stamp(pix.ToTex("wardrobe"), wood, door, wood);
        });
    }

    public static Stamp Slash()
    {
        return Cache("slash", () =>
        {
            var pix = new Pix(20, 8);
            var claw = Pix.C(255, 244, 230);
            pix.Rect(2, 3, 15, 2, claw);
            pix.Rect(4, 2, 4, 1, claw);
            pix.Rect(10, 5, 5, 1, claw);
            return new Stamp(pix.ToTex("slash"), claw, claw, claw);
        });
    }

    public static Stamp Spark()
    {
        return Cache("spark", () =>
        {
            var pix = new Pix(4, 4);
            var c = Pix.C(255, 214, 120);
            pix.Rect(1, 1, 2, 2, c);
            pix.P(0, 1, c);
            pix.P(3, 2, c);
            return new Stamp(pix.ToTex("spark"), c, c, c);
        });
    }
}
