﻿using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;
using Version = System.Version;

namespace ArithFeather.CustomItemSpawner
{
	public class CustomItemSpawner : Plugin<Config>
	{
		public static Config Configs;

		public override string Author => "Arith";
		public override Version Version => new Version(2, 8, 6);
		public override Version RequiredExiledVersion => new Version(2, 1, 3);

		private readonly Harmony _harmony = new Harmony("CustomItemSpawner");

		public delegate void PickedUpItem(ItemSpawnPoint itemSpawnPoint);
		public static event PickedUpItem OnPickedUpItem;

		public static IReadOnlyList<SavedItemType> ItemTypeList => ItemSpawnIO.ItemTypeList;
		public static IReadOnlyDictionary<string, ItemList> ItemListDictionary => ItemSpawnIO.ItemListDictionary;

		public static IReadOnlyDictionary<string, SpawnGroupData> SpawnGroupItemDictionary => ItemSpawnIO.SpawnGroupItemDictionary;
		public static IReadOnlyDictionary<string, SpawnGroupData> EndlessSpawnGroupItemDictionary => ItemSpawnIO.EndlessSpawnGroupItemDictionary;
		public static IReadOnlyDictionary<string, SpawnGroupData> ContainerGroupItemDictionary => ItemSpawnIO.ContainerGroupItemDictionary;

		public override void OnEnabled()
		{
			Configs = Config;

			ItemSpawnIO.Reload();

			_harmony.PatchAll();
			Exiled.Events.Handlers.Server.ReloadedConfigs += Server_ReloadedConfigs;
			Points.Points.OnLoadSpawnPoints += SpawnPointCreator.OnLoadSpawnPoints;

			DoorEvents.OnDoorAction += OnDoorEvent;

			Exiled.Events.Handlers.Server.WaitingForPlayers += Spawner.Instance.Reset;
			PickupDisableTrigger.OnPickedUpItem += PickupDisableTrigger_OnPickedUpItem;

			base.OnEnabled();
		}

		public override void OnDisabled()
		{
			Exiled.Events.Handlers.Server.ReloadedConfigs -= Server_ReloadedConfigs;
			Points.Points.OnLoadSpawnPoints -= SpawnPointCreator.OnLoadSpawnPoints;

			DoorEvents.OnDoorAction -= OnDoorEvent;

			Exiled.Events.Handlers.Server.WaitingForPlayers -= Spawner.Instance.Reset;
			PickupDisableTrigger.OnPickedUpItem -= PickupDisableTrigger_OnPickedUpItem;

			base.OnDisabled();
		}

		private void OnDoorEvent(DoorVariant variant, DoorAction action, ReferenceHub user) =>
			CheckDoorItemSpawn(variant);

		private void PickupDisableTrigger_OnPickedUpItem(ItemSpawnPoint itemSpawnPoint) =>
			OnPickedUpItem?.Invoke(itemSpawnPoint);


		private void Server_ReloadedConfigs() => ItemSpawnIO.Reload();

		/// <summary>
		/// Will attempt to spawn items for the rooms this door connects to.
		/// </summary>
		/// <param name="door"></param>
		public static void CheckDoorItemSpawn(DoorVariant door)
		{
			if (SavedItemRoom.SavedRooms.Count == 0) return;

			var customDoor = door.GetComponent<CustomDoor>();

			CheckRoom(customDoor.Room1);

			if (customDoor.HasTwoRooms)
			{
				CheckRoom(customDoor.Room2);
			}
		}

		/// <summary>
		/// Will attempt to spawn items for the room.
		/// </summary>
		/// <param name="room"></param>
		public static void CheckRoomItemSpawn(GameObject room)
		{
			if (SavedItemRoom.SavedRooms.TryGetValue(room.GetInstanceID(), out var itemRoom))
			{
				CheckRoom(itemRoom);
			}
		}

		private static void CheckRoom(SavedItemRoom savedRoom)
		{
			if (savedRoom != null && !savedRoom.HasBeenEntered)
			{
				savedRoom.HasBeenEntered = true;
				savedRoom.SpawnSavedItems();
			}
		}
	}
}
