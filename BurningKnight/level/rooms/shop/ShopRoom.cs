using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.item;
using BurningKnight.level.entities;
using BurningKnight.level.rooms.special;
using BurningKnight.level.tile;
using BurningKnight.state;
using Lens.entity;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.rooms.shop {
	public class ShopRoom : LockedRoom {
		public override void Paint(Level level) {
			Painter.Fill(level, this, 2, Tiles.RandomFloor());

			if (Random.Chance(30)) {
				var t = Tiles.Pick(Tile.Chasm, Tile.FloorC, Tile.FloorD);

				if (Random.Chance()) {
					Painter.FillEllipse(level, this, 3, t);
				} else {
					Painter.Fill(level, this, 3, t);
				}
				
				if (Random.Chance()) {
					t = Tiles.Pick(Tile.Chasm, Tile.FloorC, Tile.FloorD);

					if (Random.Chance()) {
						Painter.FillEllipse(level, this, 4, t);
					} else {
						Painter.Fill(level, this, 4, t);
					}
				}
			}

			if (Random.Chance()) {
				if (Random.Chance()) {
					PaintTunnel(level, Tile.Chasm, GetCenterRect(), true);
					PaintTunnel(level, Tiles.RandomNewFloor(), GetCenterRect());
				} else {
					PaintTunnel(level, Tiles.RandomNewFloor(), GetCenterRect(), Random.Chance());
				}
			}
			
			var stands = ValidateStands(level, GenerateStands());

			if (stands.Count == 0) {
				return;
			}
			
			foreach (var s in stands) {
				var stand = new ShopStand();
				level.Area.Add(stand);

				stand.Center = new Vector2(s.X * 16 + 8, s.Y * 16 + 8);
				stand.SetItem(Items.CreateAndAdd(Items.Generate(ItemPool.Shop), level.Area), null);
			}

			var p = stands[Random.Int(stands.Count)];
			var sk = new ShopKeeper();

			level.Area.Add(sk);
			sk.Center = new Vector2(p.X * 16 + 8, p.Y * 16 + 16);

			var points = new List<Point>();

			for (var x = Left + Random.Int(1, 3); x < Right; x += 2) {
				points.Add(new Point(x, Top + 2));
			}

			var props = new List<Entity> {
				new Gramophone()
			};

			if (Random.Chance(40 + Run.Luck * 7)) {
				props.Add(new RerollMachine());
			}

			if (Random.Chance(40 + Run.Luck * 7)) {
				props.Add(new VendingMachine());
			}

			foreach (var prop in props) {
				var pl = points[Random.Int(points.Count)];
				points.Remove(pl);
				
				level.Area.Add(prop);
				prop.CenterX = pl.X * 16 + 8 + Random.Int(-4, 4);
				prop.Bottom = pl.Y * 16;

				if (points.Count == 0) {
					break;
				}
			}
		}

		protected List<Point> ValidateStands(Level level, List<Point> stands) {
			var list = new List<Point>();

			for (var i = 0; i < stands.Count; i++) {
				var found = false;
				var p = stands[i];
				
				if (!level.CheckFor(p.X, p.Y, TileFlags.Passable)) {
					continue;
				}
				
				for (var j = i + 1; j < stands.Count; j++) {
					var s = stands[j];

					// Check if 2 stands are on the same tile
					if (s.X == p.X && s.Y == p.Y) {
						found = true;
						break;
					}

					// Check if 2 stands are too close
					foreach (var d in MathUtils.Directions) {
						if (s.X + (int) d.X == p.X && s.Y + (int) d.Y == p.Y) {
							found = true;
							break;
						}
					}
				}

				if (!found) {
					list.Add(p);
				}
			}
			
			return list;
		}

		protected virtual List<Point> GenerateStands() {
			var list = new List<Point>();

			var sides = new bool[4];
			var set = false;

			for (var i = 0; i < 4; i++) {
				if (Random.Chance(60)) {
					set = true;
					sides[i] = true;
				}
			}

			if (!set) {
				sides[Random.Int(4)] = true;
			}

			for (var x = Left + (Random.Chance() ? 2 : 3); x < Right - 2; x += 2) {
				if (sides[0]) {
					list.Add(new Point(x, Top + 2));
				}

				if (sides[1]) {
					list.Add(new Point(x, Bottom - 2));
				}
			}
			
			for (var y = Top + (Random.Chance() ? 2 : 3); y < Bottom - 2; y += 2) {
				if (sides[2]) {
					list.Add(new Point(Left + 2, y));
				}

				if (sides[3]) {
					list.Add(new Point(Right - 2, y));
				}
			}

			return list;
		}
	}
}