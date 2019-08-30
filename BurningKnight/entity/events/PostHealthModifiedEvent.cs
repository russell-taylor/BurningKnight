using Lens.entity;

namespace BurningKnight.entity.events {
	public class PostHealthModifiedEvent : Event {
		public int Amount;
		public Entity From;
		public Entity Who;
		public bool Default = true;
	}
}