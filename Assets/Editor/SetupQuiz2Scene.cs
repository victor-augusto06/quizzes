using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupQuiz2Scene
{
    public static void BuildScene()
    {
        string templatePath = "Assets/Scenes/SampleScene.unity";
        string quiz2Path = "Assets/Scenes/quiz_2.unity";

        // 1. Abrir cena template com câmera URP e iluminação 2D já configuradas
        var scene = EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Single);

        // 2. Configurar e carregar os sprites
        string charPath = "Assets/AssetPacks/Kenney_PixelPlatformer/Tiles/Characters/tile_0000.png";
        string groundPath = "Assets/AssetPacks/Kenney_PixelPlatformer/Tiles/tile_0021.png";

        ConfigureSpriteImporter(charPath);
        ConfigureSpriteImporter(groundPath);
        AssetDatabase.Refresh();

        Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(charPath);
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundPath);

        // 3. Criar Personagem (Player)
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 1f, 0f);

        SpriteRenderer playerSr = player.AddComponent<SpriteRenderer>();
        playerSr.sprite = charSprite;
        playerSr.sortingOrder = 5;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 2.5f;

        BoxCollider2D playerCol = player.AddComponent<BoxCollider2D>();
        if (charSprite != null)
        {
            playerCol.size = new Vector2(0.85f, 0.95f);
        }

        PlayerMovement movement = player.AddComponent<PlayerMovement>();

        // Objeto filho para GroundCheck posicionado na sola dos pés
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.parent = player.transform;
        groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        SerializedObject movementSo = new SerializedObject(movement);
        SerializedProperty gcProp = movementSo.FindProperty("groundCheck");
        if (gcProp != null)
        {
            gcProp.objectReferenceValue = groundCheckObj.transform;
        }
        SerializedProperty jfProp = movementSo.FindProperty("jumpForce");
        if (jfProp != null)
        {
            jfProp.floatValue = 11f;
        }
        movementSo.ApplyModifiedProperties();

        // 4. Criar Ambiente (Chão e Plataformas)
        GameObject env = new GameObject("Environment");

        // Chão principal
        CreatePlatform(env.transform, groundSprite, new Vector3(0f, -2f, 0f), new Vector3(14f, 1f, 1f), "Floor");

        // Plataformas elevadas para teste de pulo
        CreatePlatform(env.transform, groundSprite, new Vector3(-3.5f, -0.4f, 0f), new Vector3(3f, 0.6f, 1f), "Platform_Left");
        CreatePlatform(env.transform, groundSprite, new Vector3(3.5f, 0.8f, 0f), new Vector3(3f, 0.6f, 1f), "Platform_Right");

        // 5. Ajustar Câmera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
            cam.orthographicSize = 4f;
        }

        // 6. Salvar a cena quiz_2.unity
        EditorSceneManager.SaveScene(scene, quiz2Path);

        // 7. Adicionar nas configurações de build
        var scenes = EditorBuildSettings.scenes;
        bool alreadyAdded = false;
        foreach (var s in scenes)
        {
            if (s.path == quiz2Path)
            {
                alreadyAdded = true;
                break;
            }
        }
        if (!alreadyAdded)
        {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(quiz2Path, true);
            EditorBuildSettings.scenes = newScenes;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Cena quiz_2 atualizada com sucesso em: " + quiz2Path);
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 18;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void CreatePlatform(Transform parent, Sprite sprite, Vector3 position, Vector3 scale, string name)
    {
        GameObject plat = new GameObject(name);
        plat.transform.parent = parent;
        plat.transform.position = position;
        plat.transform.localScale = scale;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        plat.AddComponent<BoxCollider2D>();
    }
}
