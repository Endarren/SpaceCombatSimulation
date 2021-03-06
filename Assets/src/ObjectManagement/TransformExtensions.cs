﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Src.ObjectManagement
{
    public static class TransformExtensions
    {
        public static bool IsInvalid(this Transform transform)
        {
            return transform == null || transform.gameObject == null;
        }

        public static bool IsValid(this Transform transform)
        {
            return !IsInvalid(transform);
        }

        public static void SetColor(this Transform transform, float R, float G, float B, float A = 1, int depth = 20)
        {
            var colour = new Color(R, G, B, A);
            //Debug.Log(colour);
            //Debug.Log(transform);
            transform.SetColor(colour, depth);
        }

        public static void SetColor(this Transform transform, Color colour, int depth = 20)
        {
            //Debug.Log(colour);
            //Debug.Log(transform);
            var renderer = transform.GetComponent("Renderer") as Renderer;
            if (renderer != null)
            {
                //Debug.Log("has renderer");
                renderer.material.color = colour;
            }

            if (depth > 0)
            {
                var noChildren = transform.childCount;
                if (noChildren > 0)
                {
                    for (int i = 0; i < noChildren; i++)
                    {
                        var child = transform.GetChild(i);
                        if (child != null)
                        {
                            child.SetColor(colour, --depth);
                        }
                    }
                }
            }
        }

        public static Transform FindOldestParent(this Transform transform)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return transform;
            }
            return FindOldestParent(parent);
        }
    }
}
