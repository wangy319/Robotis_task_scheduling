package objects;

import java.util.ArrayList;

public class Machine extends AssemblyObject {

	public ArrayList<Task> tasksOutOfRange;

	public Machine(String id) {
		super(id);
		tasksOutOfRange = new ArrayList<>();
	}

	public void addOutOfRangeTask(Task t) {
		tasksOutOfRange.add(t);
	}

}
