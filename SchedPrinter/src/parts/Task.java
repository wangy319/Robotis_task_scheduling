package parts;

public class Task extends ScheduleObject {
	public int i;
	public int machine;
	public int predecessor;
	public int toolUsed;
	public Move moveTo;

	public Task(int start, int duration, int i, int machine, int predecessor,
			int toolUsed, String name, Move moveTo) {
		super(start, duration, name);
		this.toolUsed = toolUsed;
		this.i = i;
		this.machine = machine;
		this.predecessor = predecessor;
		this.moveTo = moveTo;
	}

	@Override
	public String getText() {
		return name + " s:" + start + " d:" + duration + " i:" + i + " m:"
				+ machine + " pred:" + predecessor + " toolUsed:" + toolUsed;
	}
}
