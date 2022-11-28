using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Util
{
public class AddressablesManager
{
    public bool Loaded { get; private set; }
    
    private AddressablesManager()
    {
        Load();
    }
    private static readonly Lazy<AddressablesManager> lazy = new(() => new AddressablesManager());
    public static AddressablesManager Instance => lazy.Value;
    

    public event EventHandler OnLoadComplete;
    
    private Dictionary<string, GameObject> preloadedAssets = new Dictionary<string, GameObject>();
    private AsyncOperationHandle<IList<AsyncOperationHandle>> loadingOperation;

    private void Load()
    {
        List<string> assetKeys = new List<string>();
        assetKeys.Add("MovementIndicator");

        List<AsyncOperationHandle> opList = new List<AsyncOperationHandle>();
        
        foreach (var assetKey in assetKeys)
        {
            AsyncOperationHandle<GameObject> loadAssetHandle = Addressables.LoadAssetAsync<GameObject>(assetKey);
            loadAssetHandle.Completed += obj => { preloadedAssets.Add(assetKey, obj.Result); };
            opList.Add(loadAssetHandle);
        }

        loadingOperation = Addressables.ResourceManager.CreateGenericGroupOperation(opList);
        loadingOperation.Completed += obj =>
        {
            Loaded = true;
            OnLoadComplete?.Invoke(this, EventArgs.Empty);
        };
    }

    public bool IsDone()
    {
        return loadingOperation.IsDone;
    }

    public GameObject Get(string key)
    {
        if (!IsDone()) return null;

        return preloadedAssets[key];
    }
}
}