﻿using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Common
{
    public class GameObjectPool : MonoBehaviour
    {
        private static GameObjectPool Instance;

        [SerializeField, UsedImplicitly] private Transform container;

        private readonly Dictionary<int, Stack<GameObject>> pooledGameObjectsByProtoId = new Dictionary<int, Stack<GameObject>>();
        private readonly Dictionary<GameObject, int> takenObjectProtoIds = new Dictionary<GameObject, int>();

        public void Initialize()
        {
            Instance = this;
        }

        public void Deinitialize()
        {
            foreach (var pooledObjects in pooledGameObjectsByProtoId)
                foreach (var pooledObject in pooledObjects.Value)
                    Destroy(pooledObject);

            pooledGameObjectsByProtoId.Clear();
            takenObjectProtoIds.Clear();
            Instance = null;
        }

        private bool ProcessReturn(GameObject returnedObject, bool destroyed)
        {
            if (!takenObjectProtoIds.TryGetValue(returnedObject, out int protoId))
                return false;

            takenObjectProtoIds.Remove(returnedObject);
            if (destroyed)
                return false;

            returnedObject.SetActive(false);
            returnedObject.transform.parent = container;

            if (pooledGameObjectsByProtoId.TryGetValue(protoId, out Stack<GameObject> pooledObjects))
                pooledObjects.Push(returnedObject);
            else
            {
                pooledObjects = new Stack<GameObject>();
                pooledObjects.Push(returnedObject);
                pooledGameObjectsByProtoId.Add(protoId, pooledObjects);
            }

            return true;
        }

        private T TakeOrCreate<T>(T prototype, Vector3 position, Quaternion rotation, Transform parent) where T : MonoBehaviour
        {
            return TakeOrCreate(prototype.gameObject, position, rotation, parent).GetComponent<T>();
        }

        private GameObject TakeOrCreate(GameObject prototype, Vector3 position, Quaternion rotation, Transform parent)
        {
            int protoId = prototype.GetInstanceID();
            GameObject newObject = TakeIfAvailable(protoId, position, rotation, parent);
            if (newObject == null)
            {
                newObject = Instantiate(prototype, position, rotation, parent);
                takenObjectProtoIds.Add(newObject, protoId);
            }

            return newObject;
        }
        
        private GameObject TakeIfAvailable(int protoId, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (pooledGameObjectsByProtoId.TryGetValue(protoId, out Stack<GameObject> pooledObjects))
            {
                while (pooledObjects.Count > 0)
                {
                    GameObject objectToTake = pooledObjects.Pop();
                    if (objectToTake == null)
                        continue;

                    objectToTake.transform.position = position;
                    objectToTake.transform.rotation = rotation;
                    objectToTake.transform.parent = parent ?? container;
                    objectToTake.SetActive(true);

                    takenObjectProtoIds.Add(objectToTake, protoId);
                    return objectToTake;
                }
            }
            return null;
        }

        public static GameObject Take(GameObject prototype, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Instance != null ? Instance.TakeOrCreate(prototype, position, rotation, parent) : Instantiate(prototype, position, rotation, parent);
        }

        public static T Take<T>(T prototype, Vector3 position, Quaternion rotation, Transform parent = null) where T: MonoBehaviour
        {
            return Instance != null ? Instance.TakeOrCreate(prototype, position, rotation, parent) : Instantiate(prototype, position, rotation, parent);
        }

        public static void Return(GameObject takenObject, bool destroyed)
        {
            bool returnedSuccessfully = Instance?.ProcessReturn(takenObject, destroyed) ?? false;
            if (!returnedSuccessfully && !destroyed)
                Destroy(takenObject);
        }

        public static void Return<T>(T takenObject, bool destroyed) where T: MonoBehaviour
        {
            Return(takenObject.gameObject, destroyed);
        }
    }
}
