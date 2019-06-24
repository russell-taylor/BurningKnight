using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.save;
using Lens.entity;

namespace BurningKnight.entity.orbital {
	public delegate void OrbitalCollisionHandler(Orbital o, Entity e);
	
	public class Orbital : SaveableEntity {
		public OrbitalCollisionHandler OnCollision;
		
		public override void AddComponents() {
			base.AddComponents();
			
			AddComponent(new OrbitalComponent());
		}

		public override bool HandleEvent(Event e) {
			if (e is CollisionStartedEvent cse) {
				OnCollision?.Invoke(this, cse.Entity);
			}

			return base.HandleEvent(e);
		}
	}
}