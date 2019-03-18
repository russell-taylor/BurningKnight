using System.Collections.Generic;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.use;
using BurningKnight.entity.projectile;
using Lens.entity;
using Lens.input;
using Random = Lens.util.math.Random;

namespace BurningKnight.entity.item {
	public static class ItemRegistry {
		public static Dictionary<string, ItemInfo> Items = new Dictionary<string, ItemInfo>();
		public static Dictionary<ItemType, List<ItemInfo>> ByType = new Dictionary<ItemType, List<ItemInfo>>();
		
		static ItemRegistry() {
			ItemInfo[] infos = {
				new ItemInfo("health_potion", () => new Item(new ModifyHpUse(30)), ItemType.Active),
			
				new ItemInfo("random_potion", () => new Item(new RandomUse(
					new ModifyHpUse(30), new ModifyHpUse(-30)
				)), ItemType.Active),
				
				new ItemInfo("bomb", () => new Item(), ItemType.Bomb),
				new ItemInfo("penny", () => new Item(), ItemType.Coin),
				new ItemInfo("key", () => new Item(), ItemType.Key),
				
				new ItemInfo("heart", () => new Item(new ModifyHpUse(2)), ItemType.Heart),
				new ItemInfo("half_heart", () => new Item(new ModifyHpUse(1)), ItemType.Heart),
				new ItemInfo("iron_heart", () => new Item(new ModifyIronHeartsUse(1)), ItemType.Heart),
				new ItemInfo("gold_heart", () => new Item(new ModifyGoldHeartsUse(1)), ItemType.Heart),
				
				new ItemInfo("sword", () => new Item(
					new MovingAngledRenderer(180, true), 
					new MeleeArcUse(1, 0.2f)
				)),
				
				new ItemInfo("gun", () => new Item(
					new AngledRenderer(), 
					new SimpleShootUse("small", 60f)
				)),
				
				new ItemInfo("halo", () => new Item(new ModifyMaxHpUse(1)), ItemType.Artifact)
			};

			foreach (var pair in infos) {
				Items[pair.Id] = pair;
			}
		}
		
		public static void Add(ItemInfo info) {
			Items[info.Id] = info;

			if (ByType.TryGetValue(info.Type, out var rooms)) {
				rooms.Add(info);
			} else {
				rooms = new List<ItemInfo>();
				rooms.Add(info);
				
				ByType[info.Type] = rooms;
			}
		}

		public static void Remove(string id) {
			if (!Items.TryGetValue(id, out var item)) {
				return;
			}
			
			Items.Remove(id);
			ByType[item.Type].Remove(item);
		}

		public static Item Create(string id, Area area = null) {
			if (!Items.TryGetValue(id, out var info)) {
				return null;
			}

			var item = CreateFrom(info);

			if (area != null) {
				area.Add(item);
				item.AddDroppedComponents();
			}

			return item;
		}
		
		public static Item CreateFrom(ItemInfo info) {
			var item = info.Create();
			
			item.Id = info.Id;
			item.Type = info.Type;
			
			return item;
		}

		public static Item Generate(ItemType type, PlayerClass c = PlayerClass.Any) {
			if (!ByType.TryGetValue(type, out var types)) {
				return null;
			}
			
			float sum = 0;

			foreach (var chance in types) {
				sum += chance.Chance.Calculate(c);
			}

			float value = Random.Float(sum);
			sum = 0;

			foreach (var t in types) {
				sum += t.Chance.Calculate(c);

				if (value < sum) {
					return CreateFrom(t);
				}
			}

			return null;
		}
	}
}