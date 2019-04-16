using System;
using System.Collections.Generic;
using BurningKnight.entity.creature.mob;
using BurningKnight.level.floors;
using BurningKnight.level.rooms.boss;
using BurningKnight.level.rooms.connection;
using BurningKnight.level.rooms.entrance;
using BurningKnight.level.rooms.secret;
using BurningKnight.level.rooms.shop;
using BurningKnight.level.rooms.special;
using BurningKnight.level.rooms.treasure;
using BurningKnight.level.tile;
using BurningKnight.level.walls;
using BurningKnight.state;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens.util;
using Microsoft.Xna.Framework;
using Random = Lens.util.math.Random;

namespace BurningKnight.level.rooms {
	public abstract class RoomDef : Rect {
		public enum Connection {
			All,
			Left,
			Right,
			Top,
			Bottom
		}

		public Dictionary<RoomDef, DoorPlaceholder> Connected = new Dictionary<RoomDef, DoorPlaceholder>();
		public int Id;

		public List<RoomDef> Neighbours = new List<RoomDef>();
		private List<Vector2> Busy = new List<Vector2>();

		public virtual int GetMinWidth() {
			return 10;
		}

		public virtual int GetMinHeight() {
			return 10;
		}

		public virtual int GetMaxWidth() {
			return 16;
		}

		public virtual int GetMaxHeight() {
			return 16;
		}

		public abstract int GetMaxConnections(Connection Side);

		public abstract int GetMinConnections(Connection Side);

		protected virtual void Fill(Level level) {
			Painter.Fill(level, this, Tile.WallA);
		}
		
		public virtual void PaintFloor(Level level) {
			Fill(level);
			FloorRegistry.Paint(level, this);
		}
		
		public virtual void Paint(Level level) {
			WallRegistry.Paint(level, this);
			
			foreach (var door in Connected.Values) {
				door.Type = DoorPlaceholder.Variant.Regular;
			}
		}

		public int GetCurrentConnections(Connection Direction) {
			if (Direction == Connection.All) {
				return Connected.Count;
			}

			var Total = 0;

			foreach (var R in Connected.Keys) {
				var I = Intersect(R);

				if (Direction == Connection.Left && I.GetWidth() == 0 && I.Left == Left) {
					Total++;
				} else if (Direction == Connection.Top && I.GetHeight() == 0 && I.Top == Top) {
					Total++;
				} else if (Direction == Connection.Right && I.GetWidth() == 0 && I.Right == Right) {
					Total++;
				} else if (Direction == Connection.Bottom && I.GetHeight() == 0 && I.Bottom == Bottom) {
					Total++;
				}
			}

			return Total;
		}

		public int GetLastConnections(Connection Direction) {
			if (GetCurrentConnections(Connection.All) >= GetMaxConnections(Connection.All)) {
				return 0;
			}

			return GetMaxConnections(Direction) - GetCurrentConnections(Direction);
		}

		public virtual bool CanConnect(Vector2 P) {
			return ((int) P.X == Left || (int) P.X == Right) != ((int) P.Y == Top || (int) P.Y == Bottom);
		}

		public bool CanConnect(Connection Direction) {
			var Cnt = GetLastConnections(Direction);

			return Cnt > 0;
		}

		public virtual bool CanConnect(RoomDef R) {
			var I = Intersect(R);
			var FoundPoint = false;

			foreach (var P in I.GetPoints()) {
				if (CanConnect(P) && R.CanConnect(P)) {
					FoundPoint = true;

					break;
				}
			}

			if (!FoundPoint) {
				return false;
			}

			if (I.GetWidth() == 0 && I.Left == Left) {
				return CanConnect(Connection.Left) && R.CanConnect(Connection.Left);
			}

			if (I.GetHeight() == 0 && I.Top == Top) {
				return CanConnect(Connection.Top) && R.CanConnect(Connection.Top);
			}

			if (I.GetWidth() == 0 && I.Right == Right) {
				return CanConnect(Connection.Right) && R.CanConnect(Connection.Right);
			}

			if (I.GetHeight() == 0 && I.Bottom == Bottom) {
				return CanConnect(Connection.Bottom) && R.CanConnect(Connection.Bottom);
			}

			return false;
		}

		public bool ConnectTo(RoomDef Other) {
			if (Neighbours.Contains(Other)) {
				return true;
			}

			var I = Intersect(Other);
			var W = I.GetWidth();
			var H = I.GetHeight();

			if (W == 0 && H >= 2 || H == 0 && W >= 2) {
				Neighbours.Add(Other);
				Other.Neighbours.Add(this);

				return true;
			}

			return false;
		}

		public bool ConnectWithRoom(RoomDef roomDef) {
			if ((Neighbours.Contains(roomDef) || ConnectTo(roomDef)) && !Connected.ContainsKey(roomDef) && CanConnect(roomDef)) {
				Connected[roomDef] = null;
				roomDef.Connected[this] = null;

				return true;
			}

			return false;
		}

		public Vector2 GetRandomCell() {
			return new Vector2(Random.Int(Left + 1, Right), Random.Int(Top + 1, Bottom));
		}
		
		public Vector2 GetRandomCellWithWalls() {
			return new Vector2(Random.Int(Left, Right + 1), Random.Int(Top, Bottom + 1));
		}

		public Vector2? GetRandomFreeCell() {
			Vector2 Point;
			var At = 0;

			do {
				if (At++ > 200) {
					Log.Error("To many attempts");

					return null;
				}

				Point = GetRandomCell();
			} while (!Run.Level.CheckFor((int) Point.X, (int) Point.Y, TileFlags.Passable));

			return Point;
		}
		
		public Vector2? GetRandomCellNearWall() {
			Vector2 Point;
			var Att = 0;

			do {
				var At = 0;

				if (At++ > 200) {
					Log.Error("To many attempts");
					return null;
				}

				while (true) {
					if (At++ > 200) {
						Log.Error("To many attempts");
						return null;
					}

					var found = false;
					Point = GetRandomCellWithWalls();

					foreach (var b in Busy) {
						if ((int) b.X == (int) Point.X && (int) b.Y == (int) Point.Y) {
							found = true;
							break;
						}
					}

					if (found) {
						continue;
					}

					if (Connected.Count == 0) {
						return Point;
					}

					if (!Run.Level.Get((int) Point.X, (int) Point.Y).IsWall()) {
						continue;
					}

					foreach (var Door in Connected.Values) {
						var Dx = (int) (Door.X - Point.X);
						var Dy = (int) (Door.Y - Point.Y);
						var D = (float) Math.Sqrt(Dx * Dx + Dy * Dy);

						if (D < 3) {
							found = true;
							break;
						}
					}

					if (!found) {
						break;
					}
				}

				if (Point.X + 1 < Right && !Run.Level.Get((int) Point.X + 1, (int) Point.Y).IsWall()) {
					Busy.Add(Point);
					return new Vector2(Point.X + 1, Point.Y);
				}
				
				if (Point.X - 1 > Left && !Run.Level.Get((int) Point.X - 1, (int) Point.Y).IsWall()) {
					Busy.Add(Point);
					return new Vector2(Point.X - 1, Point.Y);
				}
				
				if (Point.Y + 1 < Bottom && !Run.Level.Get((int) Point.X, (int) Point.Y + 1).IsWall()) {
					Busy.Add(Point);
					return new Vector2(Point.X, Point.Y + 1);
				}
				
				if (Point.X - 1 > Top && !Run.Level.Get((int) Point.X, (int) Point.Y - 1).IsWall()) {
					Busy.Add(Point);
					return new Vector2(Point.X, Point.Y - 1);
				}
			} while (true);
		}

		public Vector2? GetRandomDoorFreeCell() {
			Vector2 Point;
			var At = 0;

			while (true) {
				if (At++ > 200) {
					Log.Error("To many attempts");
					return null;
				}

				Point = GetRandomCell();

				if (Connected.Count == 0) {
					return Point;
				}

				if (!Run.Level.CheckFor((int) Point.X, (int) Point.Y, TileFlags.Passable)) {
					continue;
				}

				var found = false;

				foreach (var Door in Connected.Values) {
					var Dx = (int) (Door.X - Point.X);
					var Dy = (int) (Door.Y - Point.Y);
					var D = (float) Math.Sqrt(Dx * Dx + Dy * Dy);

					if (D < 3) {
						found = true;
						break;
					}
				}

				if (!found) {
					return Point;
				}
			}
		}

		public RoomDef GetRandomNeighbour() {
			return Neighbours[Random.Int(Neighbours.Count)];
		}

		public bool SetSize() {
			return SetSize(GetMinWidth(), GetMaxWidth(), GetMinHeight(), GetMaxHeight());
		}

		protected virtual int ValidateWidth(int W) {
			return W;
		}

		protected virtual int ValidateHeight(int H) {
			return H;
		}

		protected bool SetSize(int MinW, int MaxW, int MinH, int MaxH) {
			if (MinW < GetMinWidth() || MaxW > GetMaxWidth() || MinH < GetMinHeight() || MaxH > GetMaxHeight() || MinW > MaxW || MinH > MaxH) {
				return false;
			}

			if (Quad()) {
				var V = Math.Min(ValidateWidth(Random.Int(MinW, MaxW) - 1), ValidateHeight(Random.Int(MinH, MaxH) - 1));
				Resize(V, V);
			} else {
				Resize(ValidateWidth(Random.Int(MinW, MaxW) - 1), ValidateHeight(Random.Int(MinH, MaxH) - 1));
			}


			return true;
		}

		protected bool Quad() {
			return false;
		}

		public bool SetSizeWithLimit(int W, int H) {
			if (W < GetMinWidth() || H < GetMinHeight()) {
				return false;
			}

			SetSize();

			if (GetWidth() > W || GetHeight() > H) {
				var Ww = ValidateWidth(Math.Min(GetWidth(), W) - 1);
				var Hh = ValidateHeight(Math.Min(GetHeight(), H) - 1);

				if (Ww >= W || Hh >= H) {
					return false;
				}

				Resize(Ww, Hh);
			}

			return true;
		}

		public void ClearConnections() {
			foreach (var R in Neighbours) {
				R.Neighbours.Remove(this);
			}

			Neighbours.Clear();

			foreach (var R in Connected.Keys) {
				R.Connected.Remove(this);
			}

			Connected.Clear();
		}

		public bool CanPlaceWater(Vector2 P) {
			return Inside(P);
		}

		public List<Vector2> WaterPlaceablePoints() {
			var Points = new List<Vector2>();

			for (var I = Left + 1; I <= Right - 1; I++)
			for (var J = Top + 1; J <= Bottom - 1; J++) {
				var P = new Vector2(I, J);

				if (CanPlaceWater(P)) {
					Points.Add(P);
				}
			}

			return Points;
		}

		public bool CanPlaceGrass(Vector2 P) {
			return Inside(P);
		}

		public List<Vector2> GrassPlaceablePoints() {
			var Points = new List<Vector2>();

			for (var I = Left + 1; I <= Right - 1; I++)
			for (var J = Top + 1; J <= Bottom - 1; J++) {
				var P = new Vector2(I, J);

				if (CanPlaceGrass(P)) {
					Points.Add(P);
				}
			}

			return Points;
		}

		public override int GetWidth() {
			return base.GetWidth() + 1;
		}

		public override int GetHeight() {
			return base.GetHeight() + 1;
		}

		public Vector2 GetCenter() {
			return new Vector2(Left + GetWidth() / 2f, Top + GetHeight() / 2f);
		}

		public Rect GetConnectionSpace() {
			var C = GetDoorCenter();

			return new Rect(C.X, C.Y, C.X, C.Y);
		}

		protected Point GetDoorCenter() {
			var DoorCenter = new Point(0, 0);

			foreach (var Door in Connected.Values) {
				DoorCenter.X += Door.X;
				DoorCenter.Y += Door.Y;
			}

			var N = Connected.Count;
			var C = new Point(DoorCenter.X / N, DoorCenter.Y / N);

			if (Random.Float() < DoorCenter.X % 1) {
				C.X++;
			}

			if (Random.Float() < DoorCenter.Y % 1) {
				C.Y++;
			}

			C.X = (int) MathUtils.Clamp(Left + 1, Right - 1, C.X);
			C.Y = (int) MathUtils.Clamp(Top + 1, Bottom - 1, C.Y);

			return C;
		}

		public void PaintTunnel(Level Level, Tile Floor, Rect space = null, bool Bold = false, bool shift = true) {
			if (Connected.Count == 0) {
				Log.Error("Invalid connection room");

				return;
			}

			var C = space ?? GetConnectionSpace();

			foreach (var Door in Connected.Values) {
				var Start = new Vector2(Door.X, Door.Y);
				Vector2 Mid;
				Vector2 End;

				if (shift) {
					if ((int) Start.X == Left) {
						Start.X++;
					} else if ((int) Start.Y == Top) {
						Start.Y++;
					} else if ((int) Start.X == Right) {
						Start.X--;
					} else if ((int) Start.Y == Bottom) {
						Start.Y--;
					}
				}

				int RightShift;
				int DownShift;

				if (Start.X < C.Left) {
					RightShift = (int) (C.Left - Start.X);
				} else if (Start.X > C.Right) {
					RightShift = (int) (C.Right - Start.X);
				} else {
					RightShift = 0;
				}

				if (Start.Y < C.Top) {
					DownShift = (int) (C.Top - Start.Y);
				} else if (Start.Y > C.Bottom) {
					DownShift = (int) (C.Bottom - Start.Y);
				} else {
					DownShift = 0;
				}

				if (Door.X == Left || Door.X == Right) {
					Mid = new Vector2(Start.X + RightShift, Start.Y);
					End = new Vector2(Mid.X, Mid.Y + DownShift);
				} else {
					Mid = new Vector2(Start.X, Start.Y + DownShift);
					End = new Vector2(Mid.X + RightShift, Mid.Y);
				}

				Painter.DrawLine(Level, Start, Mid, Floor, Bold);
				Painter.DrawLine(Level, Mid, End, Floor, Bold);
			}
		}

		public virtual float WeightMob(MobInfo info, SpawnChance chance) {
			return chance.Chance;
		}

		public virtual void ModifyMobList(List<MobInfo> infos) {
			
		}

		public virtual bool ShouldSpawnMobs() {
			return false;
		}

		public static RoomType DecideType(Type room) {
			if (typeof(ExitRoom).IsAssignableFrom(room)) {
				return RoomType.Exit;
			}
			
			if (typeof(EntranceRoom).IsAssignableFrom(room)) {
				return RoomType.Entrance;
			}
			
			if (typeof(BossRoom).IsAssignableFrom(room)) {
				return RoomType.Boss;
			}
			
			if (typeof(SecretRoom).IsAssignableFrom(room)) {
				return RoomType.Secret;
			}
			
			if (typeof(TreasureRoom).IsAssignableFrom(room)) {
				return RoomType.Treasure;
			}
			
			if (typeof(SpecialRoom).IsAssignableFrom(room)) {
				return RoomType.Special;
			}
			
			if (typeof(ConnectionRoom).IsAssignableFrom(room)) {
				return RoomType.Connection;
			}
			
			if (typeof(ShopRoom).IsAssignableFrom(room)) {
				return RoomType.Shop;
			}

			return RoomType.Regular;
		}
	}
}