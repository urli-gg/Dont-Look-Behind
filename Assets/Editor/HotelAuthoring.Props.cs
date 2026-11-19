using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal sealed partial class HotelAuthoring
{
    private void Props()
    {
        var table = new HotelGeometry().Box("Dark oak", 0f, 0.73f, 0f, 0.78f, 0.085f, 0.49f, 1.4f)
            .Box("Brass patina", 0f, 0.681f, 0f, 0.721f, 0.021f, 0.445f);
        foreach (float x in new[] { -0.30f, 0.30f })
        foreach (float z in new[] { -0.162f, 0.162f })
            table.Lathe("Dark oak", new Vector3(x, 0f, z),
                Profile(0.034f,0f, 0.044f,0.06f, 0.029f,0.18f, 0.024f,0.3f, 0.048f,0.46f, 0.028f,0.55f, 0.037f,0.71f), 8);
        Architectural("Oak side table", table, new Vector3(0f, 0.38f, 0f), new Vector3(0.75f, 0.76f, 0.44f));

        var landscape = Frame().Box("The grounds at dusk", 0f, 0f, -0.027f, 0.53f, 0.61f, 0.01f, 1.6f);
        SavePrefab("Framed landscape", Model("Framed landscape", landscape));
        SavePrefab("Silver mirror", Model("Silver mirror", Frame()
            .Box("Silvered mirror", 0f, 0f, -0.027f, 0.53f, 0.61f, 0.014f)));
        SavePrefab("Blocked window", Model("Blocked window", Frame()
            .Box("Black enamel", 0f, 0f, -0.019f, 0.53f, 0.61f, 0.014f)
            .Box("Dark oak", 0f, 0.07f, -0.049f, 0.034f, 0.6f, 0.02f)
            .Box("Dark oak", 0f, 0.09f, -0.064f, 0.58f, 0.035f, 0.029f)
            .Box("Dark oak", 0f, -0.15f, -0.064f, 0.58f, 0.035f, 0.029f)));

        var vent = new HotelGeometry().Box("Black enamel", 0f, 0f, 0f, 0.47f, 0.30f, 0.03f);
        for (int i = 0; i < 7; i++)
            vent.Box("Oxidized steel", 0f, -0.113f + i * 0.037f, -0.026f, 0.43f, 0.013f, 0.028f);
        SavePrefab("Service grille", Model("Service grille", vent));

        var caseMesh = new HotelGeometry().Box("Dark oak", 0f, 0.195f, 0f, 0.54f, 0.38f, 0.20f, 1.65f)
            .Box("Brass", 0f, 0.395f, 0f, 0.15f, 0.035f, 0.046f);
        foreach (float x in new[] { -0.16f, 0.16f })
            caseMesh.Box("Black enamel", x, 0.19f, -0.005f, 0.041f, 0.385f, 0.217f)
                .Box("Brass", x, 0.28f, -0.122f, 0.037f, 0.051f, 0.024f);
        SavePrefab("Abandoned suitcase", Model("Abandoned suitcase", caseMesh));

        var clock = new HotelGeometry().Box("Dark oak", 0f, 0f, 0f, 0.31f, 0.68f, 0.105f)
            .Box("Brass", 0f, 0.17f, -0.073f, 0.225f, 0.225f, 0.041f)
            .Box("Ivory ceramic", 0f, 0.17f, -0.10f, 0.197f, 0.197f, 0.022f)
            .Box("Black enamel", -0.017f, 0.168f, -0.115f, 0.06f, 0.011f, 0.008f)
            .Box("Black enamel", 0.013f, 0.202f, -0.115f, 0.012f, 0.08f, 0.008f)
            .Box("Brass", 0f, -0.12f, -0.080f, 0.04f, 0.19f, 0.025f);
        SavePrefab("Stopped clock", Model("Stopped clock", clock));
        SavePrefab("Guest register", Model("Guest register", new HotelGeometry()
            .Box("Safety red", 0f, 0.016f, 0f, 0.31f, 0.028f, 0.37f, 2f)
            .Box("Ledger paper", 0f, 0.034f, 0f, 0.285f, 0.015f, 0.35f, 2.9f)
            .Box("Dark oak", 0f, 0.043f, 0f, 0.023f, 0.011f, 0.35f)));

        var letter = Model("Evacuation note", new HotelGeometry()
            .Box("Ledger paper", 0f, 0.007f, 0f, 0.245f, 0.014f, 0.32f, 3.1f)
            .Box("Safety red", 0.075f, 0.016f, -0.102f, 0.041f, 0.005f, 0.032f));
        var text = Label(letter.transform, "Nota del turno", "CAMBIO DE TURNO\nSi deja de oír sus pasos,\ncuente hasta tres.\nNo nuble los espejos.",
            new Vector3(0f, 0.016f, 0.014f), 0.215f, 0.235f, 0.127f, "311F14");
        text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ConfigureGrab(letter, new Vector3(0.244f, 0.018f, 0.315f), Vector3.up * 0.008f, 0.08f);
        SavePrefab("Evacuation note", letter);

        var key = Model("Service key", new HotelGeometry()
            .Box("Brass", 0f, 0.009f, 0.002f, 0.019f, 0.011f, 0.11f)
            .Box("Brass", 0f, 0.009f, 0.061f, 0.068f, 0.013f, 0.041f)
            .Box("Brass patina", 0f, 0.017f, 0.061f, 0.041f, 0.004f, 0.024f)
            .Box("Brass", 0.013f, 0.009f, -0.046f, 0.031f, 0.011f, 0.016f)
            .Box("Brass", 0.012f, 0.009f, -0.024f, 0.025f, 0.011f, 0.009f));
        ConfigureGrab(key, new Vector3(0.068f, 0.02f, 0.13f), new Vector3(0f, 0.009f, 0.01f), 0.12f);
        SavePrefab("Service key", key);

        SavePrefab("Water damage", Model("Water damage", new HotelGeometry()
            .Box("Stain", -0.035f, 0.084f, 0f, 0.16f, 0.18f, 0.002f)
            .Box("Stain", -0.085f, -0.045f, 0f, 0.014f, 0.24f, 0.002f)
            .Box("Stain", -0.052f, -0.044f, 0f, 0.015f, 0.38f, 0.002f)
            .Box("Stain", -0.012f, -0.084f, 0f, 0.023f, 0.25f, 0.002f)
            .Box("Stain", 0.03f, -0.015f, 0f, 0.022f, 0.19f, 0.002f)));

        Vessel("Ceramic vase", "Ceramic", Profile(0f,0f, .10f,0f, .11f,.035f, .14f,.13f, .13f,.21f, .087f,.26f, .07f,.33f, .082f,.365f, .066f,.365f, .054f,.33f, .074f,.25f, .109f,.19f, .093f,.035f, 0f,.03f), 0.28f, 0.37f, false);
        Vessel("Hotel bottle", "Bottle glass", Profile(0f,0f, .046f,0f, .052f,.018f, .054f,.18f, .036f,.216f, .018f,.238f, .018f,.302f, .023f,.313f, .015f,.321f, .011f,.303f, .012f,.24f, .043f,.178f, .042f,.016f, 0f,.016f), 0.11f, 0.32f, true);
        Vessel("Room tumbler", "Tumbler glass", Profile(0f,0f, .036f,0f, .038f,.016f, .052f,.111f, .046f,.111f, .031f,.017f, 0f,.015f), 0.104f, 0.112f, true);
        Vessel("Porcelain finial", "Ivory ceramic", Profile(0f,0f, .065f,0f, .07f,.018f, .042f,.042f, .083f,.094f, .06f,.153f, .028f,.175f, .018f,.204f, 0f,.227f), 0.166f, 0.23f, false);

        var frame = Frame().Box("The grounds at dusk", 0f, 0f, -0.027f, 0.53f, 0.61f, 0.01f, 1.6f);
        GameObject article = Node("Photo frame");
        var insert = Model("Photo insert", frame, article.transform);
        insert.transform.localScale = Vector3.one * 0.38f;
        insert.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        var pieces = Node("Photo frame fragments");
        for (int i = 0; i < 6; i++)
        {
            string material = i < 3 ? "Dark oak" : "Tumbler glass";
            float w = i < 3 ? 0.13f : 0.07f;
            var fragment = Model($"Frame fragment {i + 1}", new HotelGeometry().Box(material, 0f, 0f, 0f, w, 0.042f, 0.016f), pieces.transform);
            fragment.transform.localPosition = new Vector3((i % 3 - 1) * 0.062f, 0.09f + i * 0.027f, 0f);
            fragment.transform.localRotation = Quaternion.Euler(0f, 0f, 17f + i * 28f);
            DebrisBody(fragment, new Vector3(w, 0.042f, 0.016f), Vector3.zero);
        }
        FinishBreakable("Photo frame", article, pieces, new Vector3(0.26f, 0.285f, 0.055f), new Vector3(0f, 0.15f, 0f), true);
    }

    private static HotelGeometry Frame() => new HotelGeometry()
        .Box("Dark oak", 0f, 0f, 0f, 0.69f, 0.77f, 0.043f, 1.2f)
        .Box("Brass", 0f, 0.327f, -0.031f, 0.585f, 0.017f, 0.025f)
        .Box("Brass", 0f, -0.327f, -0.031f, 0.585f, 0.017f, 0.025f)
        .Box("Brass", 0.279f, 0f, -0.031f, 0.017f, 0.658f, 0.025f)
        .Box("Brass", -0.279f, 0f, -0.031f, 0.017f, 0.658f, 0.025f);

    private void Vessel(string name, string material, Vector2[] profile, float width, float height, bool glass)
    {
        GameObject article = Model(name, new HotelGeometry().Lathe(material, Vector3.zero, profile, 20));
        GameObject pieces = Node(name + " fragments");
        for (int i = 0; i < 6; i++)
        {
            float from = 60f * i, to = from + 57f;
            var fragment = Model($"{name} shard {i + 1}", new HotelGeometry().Lathe(material, Vector3.zero, profile, 4, from, to, true), pieces.transform);
            Vector3 bounds = fragment.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            Vector3 center = fragment.GetComponent<MeshFilter>().sharedMesh.bounds.center;
            DebrisBody(fragment, bounds, center);
        }
        FinishBreakable(name, article, pieces, new Vector3(width * 0.9f, height * 0.97f, width * 0.9f), Vector3.up * height * 0.5f, glass);
    }

    private static void DebrisBody(GameObject fragment, Vector3 bounds, Vector3 center)
    {
        fragment.layer = DebrisLayer;
        Box(fragment, center, bounds);
        var body = fragment.AddComponent<Rigidbody>();
        body.mass = 0.055f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void FinishBreakable(string name, GameObject model, GameObject pieces, Vector3 size, Vector3 center, bool glass)
    {
        Audio(pieces, glass ? "Glass" : "Ceramic", 0.70f, 12f);
        var dust = Node("Impact dust", pieces.transform, center).AddComponent<ParticleSystem>();
        var main = dust.main;
        main.loop = false;
        main.duration = 0.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.14f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.009f, 0.027f);
        main.startColor = HotelMedia.Hex(glass ? "A8CBC0" : "B8A082");
        main.gravityModifier = 0.22f;
        main.maxParticles = 14;
        var emission = dust.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;
        var particleRenderer = dust.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = materials[glass ? "Tumbler glass" : "Ivory ceramic"];
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = prefabs["Button cap"].GetComponent<MeshFilter>().sharedMesh;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;

        GameObject fracture = SavePrefab(name + " fragments", pieces);
        ConfigureGrab(model, size, center, glass ? 0.34f : 0.56f);
        var breakable = model.AddComponent<BreakableObject>();
        breakable.fracturePrefab = fracture;
        breakable.sounds = new[] { sounds[glass ? "Glass" : "Ceramic"], sounds[glass ? "GlassAlternate" : "CeramicAlternate"] };
        SavePrefab(name, model);
    }

    private void ConfigureGrab(GameObject model, Vector3 size, Vector3 center, float mass)
    {
        SetLayer(model, PropsLayer);
        BoxCollider collider = Box(model, center, size);
        var body = model.AddComponent<Rigidbody>();
        body.mass = mass;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        var grab = model.AddComponent<XRGrabInteractable>();
        grab.colliders.Add(collider);
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;
        grab.useDynamicAttach = true;
        model.AddComponent<HotelCarryable>().handlingSound = Audio(model, "Click", 0.45f, 4f);
    }

    private Transform WallArt(string asset, Transform parent, float side, float y, float z)
    {
        float yaw = side < 0f ? -90f : 90f;
        return Place(asset, parent, new Vector3(side * (HalfWidth - 0.22f), y, z), yaw).transform;
    }

    private void Stage(Transform scene)
    {
        var gallery = Node("Objetos y vestigios", scene).transform;
        var lamps = Node("Lámparas y sonido de planta", scene).transform;
        float[] stations = { -15.3f, -11.3f, -6.1f, -1f, 3.7f, 8.1f, 12.7f, 16.6f };
        for (int i = 0; i < stations.Length; i++)
        {
            float z = stations[i];
            float side = i % 2 == 0 ? 1f : -1f;
            WallArt("Bronze wall lamp", lamps, side, 2.08f, z);
            Light fixture = Lamp(lamps, "Luz local " + (i + 1), new Vector3(side * 0.99f, 2.10f, z),
                i == 7 ? "FC6B4B" : "F6B678", i == 7 ? 2.15f : 2.60f, 5.7f);
            if (i is 0 or 7)
                Dust(lamps, new Vector3(side * 0.78f, 1.76f, z));
            if (i == 1 || i == 4)
            {
                var buzz = Audio(Node("Fallo eléctrico", lamps, fixture.transform.localPosition), "Pipes", 0.48f, 7f);
                beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.FaultyLight, marker = fixture.transform, fixture = fixture, sound = buzz });
            }
        }

        foreach (float z in new[] { -13.5f, -3.9f, 6.4f, 14.1f })
            WallArt("Framed landscape", gallery, z % 3 < 0 ? -1f : 1f, 1.61f, z);
        WallArt("Stopped clock", gallery, 1f, 1.86f, -11.0f);
        WallArt("Service grille", gallery, -1f, 0.45f, -3.05f);
        WallArt("Service grille", gallery, 1f, 2.45f, 10.25f);
        WallArt("Water damage", gallery, 1f, 1.37f, -5.75f);
        WallArt("Water damage", gallery, -1f, 2.05f, 11.85f);
        WallArt("Silver mirror", gallery, 1f, 1.61f, 12.4f);
        WallArt("Blocked window", gallery, -1f, 1.56f, 2.86f);

        Place("Oak side table", gallery, new Vector3(1.28f, 0f, -14.7f), 90f);
        Place("Ceramic vase", gallery, new Vector3(1.27f, 0.785f, -14.73f));
        Place("Guest register", gallery, new Vector3(1.18f, 0.781f, -14.43f), 14f);

        Place("Oak side table", gallery, new Vector3(-3.92f, 0f, -8.50f), -90f);
        Place("Hotel bottle", gallery, new Vector3(-3.88f, 0.785f, -8.64f));
        Place("Room tumbler", gallery, new Vector3(-3.88f, 0.785f, -8.37f));
        Place("Evacuation note", gallery, new Vector3(-3.88f, 0.782f, -8.21f), -17f);
        Place("Abandoned suitcase", gallery, new Vector3(-3.73f, 0.01f, -9.85f), 21f);

        Place("Oak side table", gallery, new Vector3(3.97f, 0f, 4.22f), 90f);
        Place("Photo frame", gallery, new Vector3(3.97f, 0.79f, 4.34f), 92f);
        Place("Porcelain finial", gallery, new Vector3(3.97f, 0.79f, 4.00f));
        Place("Service key", gallery, new Vector3(3.80f, 0.784f, 4.32f), 41f);

        var abandoned = Place("Abandoned suitcase", gallery, new Vector3(-1.11f, 0.01f, 10.2f), -37f);
        var knock = Audio(Node("Golpes de tubería", lamps, new Vector3(-1.47f, 1.6f, -1.8f)), "Knock", 0.9f, 12f);
        beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.Sound, marker = knock.transform, sound = knock });
        var whisper = Audio(Node("Susurro sobre la alfombra", lamps, new Vector3(1.43f, 1.62f, 7.6f)), "Whisper", 0.68f, 7.8f);
        beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.Sound, marker = whisper.transform, sound = whisper });
        beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.Shift, marker = abandoned.transform, shift = abandoned.transform, sound = whisper, offset = new Vector3(-0.14f, 24f, 0.16f) });
        var breakable = Place("Hotel bottle", gallery, new Vector3(-1.28f, 0.79f, 13.75f));
        Place("Oak side table", gallery, new Vector3(-1.27f, 0f, 13.8f), -90f);
        beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.Drop, marker = breakable.transform, breakable = breakable.GetComponent<BreakableObject>() });

        foreach (float z in new[] { -14.77f, -3.56f, 8.7f, 16.5f })
            Audio(Node("Resonancia de cañerías", lamps, new Vector3(0f, 2.35f, z)), "Pipes", 0.23f, 6.8f, true, true, true);
    }

    private void Dust(Transform parent, Vector3 position)
    {
        var dust = Node("Polvo bajo la lámpara", parent, position).AddComponent<ParticleSystem>();
        var main = dust.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.1f, 4.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.017f, 0.065f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.0018f, 0.0048f);
        main.maxParticles = 22;
        var emission = dust.emission;
        emission.rateOverTime = 4.5f;
        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.54f, 0.87f, 1.15f);
        var particles = dust.GetComponent<ParticleSystemRenderer>();
        particles.sharedMaterial = materials["Ivory ceramic"];
        particles.renderMode = ParticleSystemRenderMode.Mesh;
        particles.mesh = prefabs["Button cap"].GetComponent<MeshFilter>().sharedMesh;
        particles.shadowCastingMode = ShadowCastingMode.Off;
    }
}
