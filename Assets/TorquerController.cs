﻿using Assets.Src.Interfaces;
using Assets.Src.ObjectManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorquerController : MonoBehaviour, IDeactivatable
{
    private bool _active;

    /// <summary>
    /// Tag to set on the torquer when it is deactivated.
    /// "Unteagged" is the correct tag for untagged objects,
    /// null (default) will not untag torquer whaen deactivated.
    /// </summary>
    public string InactiveTag = null;

    // Use this for initialization
    void Start()
    {
        Transform parent = transform.FindOldestParent();

        if (parent != transform)
        {
            NotifyParent(parent);
        }
    }

    private void NotifyParent(Transform parent)
    {
        parent.SendMessage("RegisterTorquer", transform, SendMessageOptions.DontRequireReceiver);
    }

    public void Deactivate()
    {
        //Debug.Log("Deactivating " + name);
        _active = false;
        if(!string.IsNullOrEmpty(InactiveTag))
        {
            tag = InactiveTag;
        }
    }
}
