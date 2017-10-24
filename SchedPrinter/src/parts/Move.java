package parts;

public class Move extends ScheduleObject {

	public Move(int start, int duration, String name) {
		super(start, duration, name);
	}

	@Override
	public String getText() {
		return name + " s:" + start + " d:" + duration;
	}
}
