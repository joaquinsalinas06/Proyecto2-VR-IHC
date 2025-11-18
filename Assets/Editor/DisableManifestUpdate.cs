using UnityEditor;
using UnityEditor.Android;
using System.IO;
using System.Xml;

/// <summary>
/// This script fixes the "Root element is missing" error by ensuring AndroidManifest.xml files
/// are valid before the Meta XR SDK tries to update them.
/// </summary>
public class FixManifestUpdate : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => -100; // Execute before UpdateManifestWithCodeSample (which has callbackOrder 0)

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        UnityEngine.Debug.Log("[FixManifestUpdate] Checking AndroidManifest.xml files...");
        
        // Find all AndroidManifest.xml files in the build directory
        var searchPath = Path.Combine(path, "..");
        if (!Directory.Exists(searchPath))
        {
            UnityEngine.Debug.LogWarning($"[FixManifestUpdate] Search path does not exist: {searchPath}");
            return;
        }
        
        var manifestFiles = Directory.GetFiles(searchPath, "AndroidManifest.xml", SearchOption.AllDirectories);
        
        UnityEngine.Debug.Log($"[FixManifestUpdate] Found {manifestFiles.Length} manifest file(s)");
        
        foreach (var manifestFile in manifestFiles)
        {
            try
            {
                // Check if the file is empty or invalid
                var fileInfo = new FileInfo(manifestFile);
                
                if (fileInfo.Length == 0)
                {
                    UnityEngine.Debug.LogWarning($"[FixManifestUpdate] Found empty AndroidManifest.xml at {manifestFile}. Creating minimal valid manifest.");
                    CreateMinimalManifest(manifestFile);
                    continue;
                }
                
                // Try to load the XML to verify it's valid
                var doc = new XmlDocument();
                try
                {
                    doc.Load(manifestFile);
                    UnityEngine.Debug.Log($"[FixManifestUpdate] Manifest is valid: {manifestFile}");
                }
                catch (XmlException)
                {
                    UnityEngine.Debug.LogWarning($"[FixManifestUpdate] Found invalid AndroidManifest.xml at {manifestFile}. Creating minimal valid manifest.");
                    CreateMinimalManifest(manifestFile);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[FixManifestUpdate] Error processing {manifestFile}: {ex.Message}");
            }
        }
    }
    
    private void CreateMinimalManifest(string manifestFile)
    {
        string minimalManifest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
    <application android:label=""Unity"">
    </application>
</manifest>";
        
        File.WriteAllText(manifestFile, minimalManifest);
        UnityEngine.Debug.Log($"[FixManifestUpdate] Created minimal manifest at: {manifestFile}");
    }
}
