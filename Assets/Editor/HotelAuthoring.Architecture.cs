using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal sealed partial class HotelAuthoring
{
    private const float WallBay = 2.55f;
    private const float FloorBay = 2.65f;
    private const float RoomDepth = 2.95f;
    private const float RoomSpan = 4.7f;
    private const float Portal = 1.52f;

    private void ModularAssets()
    {
        var wall = new HotelGeometry()
            .Box("Sage wallpaper", 0.045f, 1.40f, 0f, 0.12f, Ceiling, WallBay, 0.72f)
            .Box("Dark oak", 0.115f, 0.49f, 0f, 0.058f, 0.98f, WallBay, 0.85f)
            .Box("Dark oak", 0.155f, 0.08f, 0f, 0.08f, 0.16f, WallBay, 0.88f)
            .Box("Dark oak", 0.157f, 0.98f, 0f, 0.075f, 0.09f, WallBay, 0.76f)
            .Box("Brass patina", 0.196f, 1.005f, 0f, 0.009f, 0.016f, WallBay)
            .Box("Plaster", 0.135f, 2.74f, 0f, 0.10f, 0.11f, WallBay)
            .Box("Dark oak", 0.19f, 2.69f, 0f, 0.038f, 0.035f, WallBay);
        for (int i = -1; i <= 1; i++)
        {
            float z = i * 0.82f;
            wall.Box("Dark oak", 0.152f, 0.53f, z, 0.034f, 0.65f, 0.70f, 0.82f);
            wall.Box("Brass patina", 0.174f, 0.29f, z, 0.013f, 0.015f, 0.61f);
            wall.Box("Brass patina", 0.174f, 0.76f, z, 0.013f, 0.015f, 0.61f);
            foreach (float side in new[] { -0.31f, 0.31f })
                wall.Box("Brass patina", 0.174f, 0.53f, z + side, 0.013f, 0.49f, 0.015f);
        }
        Architectural("Paneled wall", wall, new Vector3(0.07f, Ceiling / 2f, 0f), new Vector3(0.25f, Ceiling, WallBay));

        var cabinPanel = new HotelGeometry()
            .Box("Oxidized steel", 0.045f, 1.41f, 0f, 0.12f, Ceiling, WallBay, 1.4f)
            .Box("Green enamel", 0.116f, 1.51f, 0f, 0.055f, 1.69f, 2.27f)
            .Box("Brass", 0.17f, 0.12f, 0f, 0.10f, 0.19f, WallBay)
            .Box("Brass patina", 0.165f, 1.01f, 0f, 0.06f, 0.075f, WallBay)
            .Box("Brass", 0.20f, 1.01f, 0f, 0.035f, 0.024f, WallBay);
        foreach (float z in new[] { -1.16f, -0.38f, 0.38f, 1.16f })
            cabinPanel.Box("Brass", 0.157f, 1.51f, z, 0.044f, 1.77f, 0.024f);
        Architectural("Lift cabin panel", cabinPanel, new Vector3(0.07f, Ceiling / 2f, 0f), new Vector3(0.24f, Ceiling, WallBay));

        FloorAsset("Gallery floor", HalfWidth * 2f, FloorBay, "Terrazzo", true);
        FloorAsset("Guest room floor", RoomDepth, RoomSpan, "Dark oak", false);
        FloorAsset("Cabin floor", HalfWidth * 2f, CabinBack * -1f + Entrance, "Terrazzo", false);
        FloorAsset("Vestibule floor", HalfWidth * 2f, ExitEnd - Gate, "Terrazzo", false);

        var roof = new HotelGeometry().Box("Plaster", 0f, Ceiling + 0.07f, 0f, 3.35f, 0.18f, FloorBay, 0.86f)
            .Box("Dark oak", 0f, 2.765f, -1.2f, 3.35f, 0.10f, 0.14f);
        foreach (float x in new[] { -1.45f, -1.35f, 1.35f, 1.45f })
            roof.Box("Plaster", x, 2.767f, 0f, 0.068f, 0.082f, FloorBay);
        Architectural("Coffered ceiling", roof, new Vector3(0f, Ceiling + 0.07f, 0f), new Vector3(3.35f, 0.18f, FloorBay));

        var column = new HotelGeometry().Box("Dark oak", 0f, 1.39f, 0f, 0.19f, 2.78f, 0.21f)
            .Box("Brass patina", 0f, 0.98f, 0f, 0.206f, 0.028f, 0.227f);
        foreach (float y in new[] { 0.08f, 0.19f, 2.59f, 2.69f, 2.75f })
            column.Box("Dark oak", 0f, y, 0f, 0.244f, 0.075f, 0.264f);
        SavePrefab("Corner pilaster", Model("Corner pilaster", column));

        var trim = new HotelGeometry();
        foreach (float side in new[] { -1f, 1f })
        {
            trim.Box("Dark oak", side * 0.82f, 1.13f, 0f, 0.17f, 2.26f, 0.24f);
            trim.Box("Brass", side * 0.743f, 1.13f, -0.122f, 0.021f, 2.26f, 0.032f);
        }
        trim.Box("Dark oak", 0f, 2.24f, 0f, 1.81f, 0.22f, 0.24f)
            .Box("Brass", 0f, 2.144f, -0.125f, 1.52f, 0.021f, 0.029f)
            .Box("Dark oak", 0f, 2.37f, 0f, 1.99f, 0.08f, 0.29f);
        SavePrefab("Guest doorway", Model("Guest doorway", trim));

        var slab = new HotelGeometry().Box("Dark oak", 0f, 1.05f, 0f, 1.36f, 2.10f, 0.09f);
        foreach (float x in new[] { -0.36f, 0.36f })
        foreach (float y in new[] { 0.46f, 1.52f })
        {
            slab.Box("Dark oak", x, y, -0.056f, 0.57f, 0.73f, 0.032f);
            slab.Box("Brass patina", x, y - 0.33f, -0.077f, 0.49f, 0.015f, 0.012f);
        }

        slab.Box("Brass", 0.54f, 0.96f, -0.067f, 0.10f, 0.23f, 0.032f)
            .Box("Brass", 0.5f, 0.98f, -0.112f, 0.17f, 0.035f, 0.073f);
        Architectural("Guest door leaf", slab, new Vector3(0f, 1.05f, 0f), new Vector3(1.36f, 2.10f, 0.10f));

        var lift = new HotelGeometry()
            .Box("Oxidized steel", 0f, 1.11f, 0f, 0.756f, 2.22f, 0.095f)
            .Box("Brass", 0f, 0.13f, -0.05f, 0.756f, 0.14f, 0.032f)
            .Box("Green enamel", 0f, 1.31f, -0.054f, 0.62f, 1.37f, 0.030f)
            .Box("Black enamel", 0f, 1.31f, -0.074f, 0.51f, 1.26f, 0.030f);
        foreach (float x in new[] { -0.23f, -0.115f, 0f, 0.115f, 0.23f })
            lift.Box("Oxidized steel", x, 1.31f, -0.098f, 0.038f, 1.23f, 0.02f);
        Architectural("Lift sliding leaf", lift, new Vector3(0f, 1.11f, 0f), new Vector3(0.756f, 2.22f, 0.12f));

        var grille = new HotelGeometry().Box("Green enamel", 0f, 1.11f, 0f, 1.70f, 2.22f, 0.16f);
        for (int i = 0; i < 16; i++)
            grille.Box(i % 4 == 0 ? "Brass patina" : "Oxidized steel", 0f, 0.095f + i * 0.134f, -0.09f, 1.70f, 0.098f, 0.061f);
        Architectural("Fire shutter", grille, new Vector3(0f, 1.11f, 0f), new Vector3(1.70f, 2.22f, 0.19f));

        var sconce = new HotelGeometry().Box("Brass", 0f, 0f, 0f, 0.17f, 0.37f, 0.059f)
            .Box("Brass", 0f, -0.07f, -0.105f, 0.071f, 0.052f, 0.20f)
            .Lathe("Warm glass", new Vector3(0f, -0.03f, -0.17f), Profile(0.040f,0f, 0.078f,0.06f, 0.091f,0.23f, 0.074f,0.26f), 10)
            .Lathe("Brass", new Vector3(0f, -0.03f, -0.17f), Profile(0f,0.246f, 0.09f,0.246f, 0.091f,0.267f, 0f,0.279f), 10);
        SavePrefab("Bronze wall lamp", Model("Bronze wall lamp", sconce));
        SavePrefab("Ceiling fixture", Model("Ceiling fixture", new HotelGeometry()
            .Lathe("Brass", Vector3.zero, Profile(0.23f,-0.09f, 0.26f,-0.055f, 0.21f,0.025f, 0f,0.04f), 20)
            .Lathe("Warm glass", Vector3.zero, Profile(0f,-0.132f, 0.14f,-0.122f, 0.225f,-0.088f, 0.227f,-0.059f), 20)));

        SavePrefab("Button cap", Model("Button cap", new HotelGeometry().Box("Green enamel", 0f, 0f, 0f, 1f, 1f, 0.034f)));
        SavePrefab("Console face", Model("Console face", new HotelGeometry().Box("Black enamel", 0f, 0f, 0f, 1f, 1f, 0.060f)));
        SavePrefab("Phosphor display", Model("Phosphor display", new HotelGeometry().Box("Phosphor", 0f, 0f, 0f, 1f, 1f, 0.023f)));
        SavePrefab("Comfort screen", Model("Comfort screen", new HotelGeometry().Box("Comfort fade", 0f, 0f, 0f, 3f, 3f, 0.001f)));
        SavePrefab("Bronze plaque", Model("Bronze plaque", new HotelGeometry()
            .Box("Brass patina", 0f, 0f, 0f, 1f, 1f, 0.04f)
            .Box("Black enamel", 0f, 0f, -0.033f, 0.955f, 0.89f, 0.03f)));
    }

    private static Vector2[] Profile(params float[] coordinates)
    {
        var points = new Vector2[coordinates.Length / 2];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector2(coordinates[2 * i], coordinates[2 * i + 1]);
        return points;
    }

    private void Architectural(string name, HotelGeometry geometry, Vector3 center, Vector3 size)
    {
        GameObject model = Model(name, geometry);
        SetLayer(model, ShellLayer);
        Box(model, center, size);
        SavePrefab(name, model);
    }

    private void FloorAsset(string name, float width, float length, string finish, bool runner)
    {
        var mesh = new HotelGeometry().Box(finish, 0f, -0.099f, 0f, width, 0.20f, length, 0.72f);
        if (runner)
            mesh.Box("Embroidered wool", 0f, 0.005f, 0f, 1.55f, 0.006f, length, 0.78f)
                .Box("Brass patina", -0.765f, 0.005f, 0f, 0.027f, 0.008f, length)
                .Box("Brass patina", 0.765f, 0.005f, 0f, 0.027f, 0.008f, length);
        Architectural(name, mesh, new Vector3(0f, -0.095f, 0f), new Vector3(width, 0.19f, length));
    }

    private GameObject Shell(string name, Transform parent, Vector3 position, float yaw = 0f, bool nav = true)
    {
        GameObject target = Place(name, parent, position, yaw);
        MarkStatic(target);
        if (nav)
            navigation.Add(target.GetComponent<BoxCollider>());
        return target;
    }

    private void WallRuns(Transform parent, float x, float start, float end, float yaw, string module = "Paneled wall", bool nav = true)
    {
        int count = Mathf.CeilToInt((end - start) / WallBay);
        float size = (end - start) / count;
        for (int i = 0; i < count; i++)
        {
            float center = start + size * (i + 0.5f);
            GameObject wall = Shell(module, parent, new Vector3(x, 0f, center), yaw, nav);
            wall.transform.localScale = new Vector3(1f, 1f, size / WallBay);
            if (module == "Paneled wall" && i > 0 && size > 1.1f)
                MarkStatic(Place("Corner pilaster", parent, new Vector3(x + (yaw == 0f ? 0.12f : -0.12f), 0f, center - size / 2f)));
        }
    }

    private void CrossWall(Transform parent, float from, float to, float z, float yaw = 90f, bool nav = true, string module = "Paneled wall")
    {
        var wall = Shell(module, parent, new Vector3((from + to) / 2f, 0f, z), yaw, nav);
        wall.transform.localScale = new Vector3(1f, 1f, (to - from) / WallBay);
    }

    private void PortalTrim(Transform parent, string name, float centerZ, float openingWidth, bool exit)
    {
        var mesh = new HotelGeometry();
        float width = (HalfWidth * 2f - openingWidth) / 2f;
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (openingWidth / 2f + width / 2f);
            mesh.Box("Oxidized steel", x, 1.12f, 0f, width, 2.24f, 0.24f)
                .Box("Dark oak", x, 1.15f, -0.132f, width - 0.11f, 2.04f, 0.045f)
                .Box("Brass", side * (openingWidth / 2f + 0.03f), 1.12f, -0.143f, 0.06f, 2.24f, 0.075f);
        }
        mesh.Box("Oxidized steel", 0f, 2.53f, 0f, HalfWidth * 2f, 0.58f, 0.31f)
            .Box("Brass", 0f, 2.243f, -0.165f, openingWidth + 0.13f, 0.08f, 0.065f);
        var template = Model(name, mesh);
        SetLayer(template, ShellLayer);
        foreach (float side in new[] { -1f, 1f })
            Box(template, new Vector3(side * (openingWidth / 2f + width / 2f), 1.12f, 0f), new Vector3(width, 2.24f, 0.27f));
        Box(template, new Vector3(0f, 2.53f, 0f), new Vector3(HalfWidth * 2f, 0.58f, 0.32f));
        SavePrefab(name, template);
        GameObject portal = Place(name, parent, new Vector3(0f, 0f, centerZ));
        MarkStatic(portal);
        foreach (var collider in portal.GetComponents<BoxCollider>())
            navigation.Add(collider);
        if (exit)
            Label(portal.transform, "Salida", "S A L I D A", new Vector3(0f, 2.55f, -0.182f), 1.2f, 0.24f, 0.46f, "FFB089");
    }

    private void Corridor(Transform parent)
    {
        int count = Mathf.CeilToInt((Gate - Entrance) / FloorBay);
        float step = (Gate - Entrance) / count;
        for (int i = 0; i < count; i++)
        {
            float center = Entrance + step * (i + 0.5f);
            var position = new Vector3(0f, 0f, center);
            var scale = new Vector3(1f, 1f, step / FloorBay);
            var floor = Shell("Gallery floor", parent, position);
            floor.transform.localScale = scale;
            Walkable(floor, teleportAreas);
            Shell("Coffered ceiling", parent, position).transform.localScale = scale;
        }

        BuildRoom(parent, -1f, -8.6f, "104");
        BuildRoom(parent, 1f, 3.85f, "107");
        PortalTrim(parent, "Lift entrance", Entrance, 1.51f, false);
        PortalTrim(parent, "Exit frame", Gate, 1.70f, true);
        var exitDeck = Shell("Vestibule floor", parent, new Vector3(0f, 0f, (Gate + ExitEnd) / 2f), nav: false);
        Walkable(exitDeck, null);
        CrossWall(parent, -HalfWidth, HalfWidth, ExitEnd, 90f, false);
        WalkableExit = exitDeck.GetComponent<TeleportationArea>();
    }

    private TeleportationArea WalkableExit { get; set; }

    private void BuildRoom(Transform parent, float side, float center, string number)
    {
        float x = side * HalfWidth;
        float low = center - Portal / 2f, high = center + Portal / 2f;
        float yaw = side < 0f ? 0f : 180f;
        WallRuns(parent, x, Entrance, low, yaw);
        WallRuns(parent, x, high, ExitEnd, yaw);
        var room = Node("Habitación " + number, parent).transform;
        var lintel = Shell("Paneled wall", room, new Vector3(x, 2.13f, center), yaw);
        lintel.transform.localScale = new Vector3(1f, 0.245f, Portal / WallBay);
        float roomX = side * (HalfWidth + RoomDepth / 2f);
        float outer = side * (HalfWidth + RoomDepth);
        float minX = Mathf.Min(x, outer), maxX = Mathf.Max(x, outer);
        CrossWall(room, minX, maxX, center - RoomSpan / 2f, -90f);
        CrossWall(room, minX, maxX, center + RoomSpan / 2f);
        WallRuns(room, outer, center - RoomSpan / 2f, center + RoomSpan / 2f, yaw);
        var floor = Shell("Guest room floor", room, new Vector3(roomX, 0f, center));
        Walkable(floor, teleportAreas);
        var ceiling = Shell("Coffered ceiling", room, new Vector3(roomX, 0f, center));
        ceiling.transform.localScale = new Vector3(RoomDepth / 3.35f, 1f, RoomSpan / FloorBay);

        float frameYaw = side < 0f ? 90f : -90f;
        var trim = Place("Guest doorway", room, new Vector3(x, 0f, center), frameYaw);
        MarkStatic(trim);
        Label(trim.transform, "Número de habitación", number, new Vector3(0f, 2.33f, -0.174f), 0.75f, 0.22f, 0.46f);
        var hinge = Node("Bisagra inferior", trim.transform, new Vector3(-0.695f, 0f, 0.018f), 103f);
        var leaf = Place("Guest door leaf", hinge.transform, new Vector3(0.685f, 0f, 0f));
        // Each guest door starts ajar, with a full accessible opening and a press to close or reopen it.
        var button = leaf.AddComponent<HotelButton>();
        button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().colliders.Add(leaf.GetComponent<BoxCollider>());
        var swing = hinge.AddComponent<HotelSwingDoor>();
        swing.leafCollider = leaf.GetComponent<BoxCollider>();
        swing.hingeSound = Audio(hinge, "Door", 0.24f, 4f);
        var obstacle = leaf.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0f, 1.05f, 0f);
        obstacle.size = new Vector3(1.36f, 2.1f, 0.21f);
        obstacle.carving = true;
        swing.obstacle = obstacle;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onPress, swing.Toggle);

        var fixture = Lamp(room, "Luz habitación " + number, new Vector3(roomX, 2.46f, center), side < 0 ? "DBA16D" : "9DC3B6", 1.85f, 5.6f);
        MarkStatic(Place("Ceiling fixture", room, new Vector3(roomX, 2.72f, center)));
        if (side > 0f)
        {
            var wallControl = Node("Interruptor de lámpara", room, new Vector3(2.18f, 1.30f, center - RoomSpan / 2f + 0.25f), 180f);
            var toggle = wallControl.AddComponent<HotelLightSwitch>();
            toggle.fixture = fixture;
            var buttonSwitch = Button(wallControl.transform, "Conmutador", "LUZ", Vector3.zero, 0.16f, 0.12f, "Brass patina", 0.28f);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(buttonSwitch.onPress, toggle.Toggle);
        }
    }
}
