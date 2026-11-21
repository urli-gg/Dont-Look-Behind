using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal sealed partial class NightmareAuthoring
{
    private void BindEvents(HorrorEventDirector director, Creature creature)
    {
        director.tension = tension;
        director.lighting = lighting;
        director.soundscape = soundscape;
        var beats = new List<HorrorEventDirector.Beat>();
        var objects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform Nearby(string name, Vector3 spot) => objects.Where(item => item.name == name)
            .OrderBy(item => (item.position - spot).sqrMagnitude).First();

        var hallBeforeRooms = new Vector3(0f, 1.33f, -13.1f);
        beats.Add(Beat("pasos-recepcion", HorrorEventDirector.Effect.Behind, hallBeforeRooms, "DistantTread", 0.53f, 9.0f, 7f));
        beats.Add(Beat("susurro-tramo-ciego", HorrorEventDirector.Effect.Behind, new Vector3(0f, 1.48f, -3.7f),
            "HushLeft", 0.62f, 6.6f, 7.5f, HorrorTensionDirector.Stage.Suspicion));
        beats.Add(Beat("respiracion-servicio", HorrorEventDirector.Effect.Behind, new Vector3(0f, 1.48f, 11.8f),
            "PursuitBreath", 0.53f, 6.9f, 7.8f, HorrorTensionDirector.Stage.Threat));

        beats.Add(Beat("pared-104", HorrorEventDirector.Effect.Sound, new Vector3(-4.42f, 1.12f, -8.95f),
            "FinalRattle", .61f, 8f, 7.2f, HorrorTensionDirector.Stage.Suspicion));
        beats.Add(Beat("madera-107", HorrorEventDirector.Effect.Sound, new Vector3(4.35f, 1.02f, 5.40f),
            "WoodUnderfoot", .61f, 5.3f, 6.3f, HorrorTensionDirector.Stage.Suspicion));
        beats.Add(Beat("tuberia-salida", HorrorEventDirector.Effect.Sound, new Vector3(-1.24f, 1.47f, 15.8f),
            "FinalRattle", .57f, 11f, 6.5f, HorrorTensionDirector.Stage.Panic));

        var flicker = Beat("resistencia-defectuosa", HorrorEventDirector.Effect.FaultyLight, fixtures[1].light.transform.position,
            "WireFail", .54f, 7f, 7.1f);
        flicker.fixture = fixtures[1].light;
        beats.Add(flicker);
        var burned = Beat("ruptura-ultima-lampara", HorrorEventDirector.Effect.FaultyLight, fixtures[6].light.transform.position,
            "WireFail", .65f, 8f, 7.5f, HorrorTensionDirector.Stage.Threat);
        burned.fixture = fixtures[6].light;
        burned.burnOut = true;
        beats.Add(burned);

        var blackout = Beat("corte-de-planta", HorrorEventDirector.Effect.Blackout, new Vector3(0.28f, 1.50f, 4.6f),
            "WireFail", .63f, 7.5f, 8.5f, HorrorTensionDirector.Stage.Threat);
        blackout.duckMix = true;
        beats.Add(blackout);

        var door = Beat("portazo-107", HorrorEventDirector.Effect.Door, new Vector3(1.6f, 1.10f, 3.55f),
            "HingeSlam", .55f, 8f, 5.6f, HorrorTensionDirector.Stage.Suspicion);
        door.door = UnityEngine.Object.FindObjectsByType<HotelSwingDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(item => item.transform.position.x > 0f);
        door.requiresRoom = true;
        beats.Add(door);

        var shift = Beat("maleta-recolocada", HorrorEventDirector.Effect.Shift, new Vector3(-1.1f, 0.18f, 10.2f),
            "WoodUnderfoot", .47f, 5.2f, 6f, HorrorTensionDirector.Stage.Suspicion);
        shift.shift = Nearby("Abandoned suitcase", shift.marker.position);
        shift.offset = new Vector3(-0.08f, 31f, 0.20f);
        beats.Add(shift);

        var fall = Beat("cuadro-desclavado", HorrorEventDirector.Effect.Fall, new Vector3(-1.23f, 1.52f, -3.9f),
            "HingeSlam", .45f, 7f, 6f, HorrorTensionDirector.Stage.Suspicion);
        fall.shift = Nearby("Framed landscape", fall.marker.position);
        fall.offset = new Vector3(0.24f, -1.24f, 0.16f);
        beats.Add(fall);

        var drop = Beat("vidrio-sin-manos", HorrorEventDirector.Effect.Drop, new Vector3(-1.28f, .82f, 13.75f),
            "WoodUnderfoot", .42f, 5f, 7f, HorrorTensionDirector.Stage.Suspicion);
        drop.breakable = Nearby("Hotel bottle", drop.marker.position).GetComponent<BreakableObject>();
        beats.Add(drop);

        var retainedScare = UnityEngine.Object.FindObjectsByType<JumpscareTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SingleOrDefault(item => item.transform.parent == null);
        if (retainedScare != null)
        {
            retainedScare.viewer = head;
            retainedScare.anticipation = .48f;
            retainedScare.jumpscareDuration = .85f;
            retainedScare.drift = new Vector3(.55f, 0f, 0f);
            Vector3 location = retainedScare.transform.position + Vector3.up * 1.1f;
            retainedScare.jumpscareAudio = Audio("Antes del cruce", location, "DistantTread", .48f, 9.5f);
            retainedScare.reveal = Audio("Presencia al cruzar", location, "HushLeft", .54f, 8f);
            retainedScare.accent = AddPoint(stage, "Destello suave de cruce", location + Vector3.up * .66f, "9BB9B4", 0f, 2.8f);
            beats.Add(new HorrorEventDirector.Beat
            {
                id = "cruce-transversal", effect = HorrorEventDirector.Effect.Apparition, marker = retainedScare.transform,
                ambientScare = retainedScare, minimumDistance = 3.3f, maximumDistance = 10.4f,
                minimumStage = HorrorTensionDirector.Stage.Suspicion, duckMix = true
            });
        }

        GameObject shape = CreateSilhouette();
        beats.Add(Silhouette("figura-junto-a-107", shape, new Vector3(.94f, 0f, 5.1f), new Vector3(.24f, 0f, .42f), 4.0f, 13f));
        beats.Add(Silhouette("figura-ante-el-cierre", shape, new Vector3(-.28f, 0f, 15.85f), new Vector3(-.42f, 0f, .21f), 5.0f, 15f));
        director.beats = beats.ToArray();
    }

    private HorrorEventDirector.Beat Beat(string id, HorrorEventDirector.Effect effect, Vector3 where, string sound,
        float volume, float range, float maximum, HorrorTensionDirector.Stage minimum = HorrorTensionDirector.Stage.Unease)
    {
        var root = Node(id, stage, where).transform;
        return new HorrorEventDirector.Beat
        {
            id = id, effect = effect, marker = root, sound = Audio("Fuente espacial", Vector3.zero, sound, volume, range, parent: root),
            minimumStage = minimum, maximumDistance = maximum
        };
    }

    private HorrorEventDirector.Beat Silhouette(string id, GameObject shape, Vector3 where, Vector3 drift, float minimum, float maximum)
    {
        var root = Node(id, stage, where).transform;
        var figure = Place(shape, root, Vector3.zero, 6f);
        figure.SetActive(false);
        var scare = root.gameObject.AddComponent<JumpscareTrigger>();
        scare.jumpscareObject = figure;
        scare.jumpscareAudio = Audio("Aviso bajo", Vector3.up * 1.24f, "WireFail", .51f, 13.5f, parent: root);
        scare.reveal = Audio("Exhalación", Vector3.up * 1.31f, "HushLeft", .42f, 11f, parent: root);
        scare.accent = AddPoint(root, "Contraluz", new Vector3(.38f, 1.87f, .62f), "93C2CA", 0f, 3f);
        scare.viewer = head;
        scare.anticipation = .56f;
        scare.jumpscareDuration = 1.05f;
        scare.drift = drift;
        return new HorrorEventDirector.Beat
        {
            id = id, effect = HorrorEventDirector.Effect.Apparition, marker = root, ambientScare = scare, duckMix = true,
            minimumStage = HorrorTensionDirector.Stage.Suspicion, minimumDistance = minimum, maximumDistance = maximum
        };
    }
}
