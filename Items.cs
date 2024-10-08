﻿namespace GibsonCrabGameGlobalOffensive
{
    public class ItemsRemover : MonoBehaviour
    {
        bool init;
        public static int deleteDelay = 100;

        void Update()
        {
            if (!init)
            {
                itemToDelete.Clear();
                init = true;
            }

            var dico = SharedObjectManager.Instance.GetDictionary(); // Récupération du dictionnaire partagé
            List<int> itemsToRemove = [];

            // Vérification des items à supprimer
            foreach (var item in itemToDelete)
            {
                if ((DateTime.Now - item.Value).TotalMilliseconds > deleteDelay)
                {
                    itemsToRemove.Add(item.Key);
                }
            }

            // Suppression des objets
            foreach (var key in itemsToRemove)
            {
                // On vérifie si la clé existe dans le dictionnaire
                if (!dico.ContainsKey(key)) continue;

                itemToDelete.Remove(key);

                // Récupération de l'objet partagé à l'aide de la clé
                var obj = SharedObjectManager.Instance.GetSharedObject(key);

                // Si l'objet n'existe pas, on continue
                if (obj == null) continue;

                // Récupération et interaction avec les composants spécifiques
                var comp1 = obj.GetComponent<ItemGun>();
                var comp2 = obj.GetComponent<MonoBehaviour1PublicInamUnique>();
                var comp3 = obj.GetComponent<MonoBehaviour2PublicTrguGamubuGaSiBoSiUnique>();
                var comp4 = obj.GetComponent<MonoBehaviour2PublicObauTrSiVeSiGahiUnique>();
                var comp5 = obj.GetComponent<MonoBehaviour2PublicUnique>();

                comp1?.TryInteract();
                comp2?.TryInteract();
                comp3?.TryInteract();
                comp4?.TryInteract();
                comp5?.TryInteract();

                // Forcer la suppression de l'item sur le serveur
                GameServer.ForceRemoveItem(clientId, key);
            }
        }
    }
}
