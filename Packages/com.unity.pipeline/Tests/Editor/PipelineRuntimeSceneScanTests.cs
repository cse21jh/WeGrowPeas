using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.Editor.BuildProcessors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Tests.Editor
{
    public class PipelineRuntimeSceneScanTests
    {
        private string m_Folder;
        private string m_ScenePath;
        private EditorBuildSettingsScene[] m_OriginalBuildScenes;
        private readonly List<Scene> m_OpenedForScan = new List<Scene>();

        [SetUp]
        public void SetUp()
        {
            m_OriginalBuildScenes = EditorBuildSettings.scenes;
            m_Folder = "Assets/__PipelineSceneScanTests_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", m_Folder.Substring("Assets/".Length));
            m_ScenePath = m_Folder + "/Test.unity";
            // Test Runner restores the user's scene setup after the run. Keep a saved empty
            // anchor open so individual test scenes can be opened and closed additively.
            var anchor = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(anchor, m_Folder + "/Anchor.unity");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            EditorSceneManager.SaveScene(scene, m_ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(m_ScenePath, true) };
        }

        [TearDown]
        public void TearDown()
        {
            EditorBuildSettings.scenes = m_OriginalBuildScenes;
            foreach (var scene in m_OpenedForScan)
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
            m_OpenedForScan.Clear();

            var testScene = SceneManager.GetSceneByPath(m_ScenePath);
            if (testScene.IsValid() && testScene.isLoaded)
                EditorSceneManager.CloseScene(testScene, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(m_Folder);
        }

        [Test]
        public void ClosedSceneWithoutManager_IsNeverOpened()
        {
            EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(m_ScenePath), true);
            Assert.IsFalse(PipelineRuntimeBuildProcessor.SceneMayContainRuntimeManager(m_ScenePath));

            var openedCount = 0;
            void OnOpened(Scene scene, OpenSceneMode mode) { openedCount++; }
            EditorSceneManager.sceneOpened += OnOpened;
            try
            {
                Assert.IsEmpty(FindManagers());
                Assert.IsEmpty(m_OpenedForScan);
                Assert.AreEqual(0, openedCount, "An unrelated scene must not run scene-load callbacks.");
            }
            finally
            {
                EditorSceneManager.sceneOpened -= OnOpened;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClosedSceneWithInactiveManager_IsStillScanned(bool useNestedPrefab)
        {
            var managerObject = new GameObject("Inactive runtime manager");
            managerObject.SetActive(false);
            managerObject.AddComponent<RuntimePipelineManager>();

            if (useNestedPrefab)
            {
                var innerPrefab = PrefabUtility.SaveAsPrefabAsset(managerObject, m_Folder + "/Inner.prefab");
                Object.DestroyImmediate(managerObject);
                var outer = new GameObject("Outer prefab");
                var innerInstance = (GameObject)PrefabUtility.InstantiatePrefab(innerPrefab);
                innerInstance.transform.SetParent(outer.transform);
                var outerPrefab = PrefabUtility.SaveAsPrefabAsset(outer, m_Folder + "/Outer.prefab");
                Object.DestroyImmediate(outer);
                PrefabUtility.InstantiatePrefab(outerPrefab);
            }

            var scene = SceneManager.GetSceneByPath(m_ScenePath);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);

            Assert.IsTrue(PipelineRuntimeBuildProcessor.SceneMayContainRuntimeManager(m_ScenePath));
            var managers = FindManagers();
            Assert.AreEqual(1, managers.Count, "Inactive managers still need configuration validation.");
            Assert.AreEqual(m_ScenePath, managers[0].gameObject.scene.path);
            Assert.AreEqual(1, m_OpenedForScan.Count);
        }

        [Test]
        public void LoadedSceneWithUnsavedManager_IsInspectedWithoutReloading()
        {
            // The saved scene has no manager; only the current, unsaved scene contains it.
            Assert.IsFalse(PipelineRuntimeBuildProcessor.SceneMayContainRuntimeManager(m_ScenePath));
            var managerObject = new GameObject("Unsaved runtime manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<RuntimePipelineManager>();
            var scene = SceneManager.GetSceneByPath(m_ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);

            var openedCount = 0;
            void OnOpened(Scene openedScene, OpenSceneMode mode) { openedCount++; }
            EditorSceneManager.sceneOpened += OnOpened;
            try
            {
                CollectionAssert.Contains(FindManagers(), manager);
                Assert.IsEmpty(m_OpenedForScan);
                Assert.AreEqual(0, openedCount);
                Assert.IsTrue(scene.isDirty, "Scanning must preserve unsaved scene changes.");
            }
            finally
            {
                EditorSceneManager.sceneOpened -= OnOpened;
            }
        }

        private List<RuntimePipelineManager> FindManagers()
        {
            var method = typeof(PipelineRuntimeBuildProcessor).GetMethod(
                "FindRuntimeManagersInBuildScenes", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (List<RuntimePipelineManager>)method.Invoke(
                new PipelineRuntimeBuildProcessor(), new object[] { m_OpenedForScan });
        }
    }
}
