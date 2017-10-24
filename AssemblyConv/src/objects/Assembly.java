package objects;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map.Entry;

import timematrix.TimeMatrix;
import timematrix.TimeMatrix3D;
import containers.ToolChangeDurations;

public class Assembly {
	private HashMap<String, Tray> trays;
	private HashMap<String, Output> outputs;
	private HashMap<String, Fixture> fixtures;
	private HashMap<String, Component> components;
	private HashMap<String, Tool> tools;
	private HashMap<String, Machine> machines;
	private HashMap<String, Task> tasks;

	private ArrayList<ArrayList<Task>> ordered;
	private ArrayList<ArrayList<Task>> concurrent;

	private ToolChangeDurations toolChangeDurations;

	private boolean doneParsing = false;

	private HashMap<Integer, Task> iToTask;
	private HashMap<Integer, Output> iToOutput;
	private HashMap<Integer, Tray> iToTray;
	private HashMap<Integer, Fixture> iToFixture;
	private HashMap<Integer, Machine> iToMachine;
	private HashMap<Integer, Component> iToComponent;
	private HashMap<Integer, Tool> iToTool;

	public String StartPos;

	public Task dummyTask;

	public TimeMatrix timeMatrix;
	public TimeMatrix3D timeMatrix3D;

	public Assembly(TimeMatrix timeMatrix) {
		trays = new HashMap<>();
		outputs = new HashMap<>();
		fixtures = new HashMap<>();
		components = new HashMap<>();
		tools = new HashMap<>();
		machines = new HashMap<>();
		tasks = new HashMap<>();

		ordered = new ArrayList<>();
		concurrent = new ArrayList<>();

		toolChangeDurations = new ToolChangeDurations();

		dummyTask = new Task("dummy", 0);

		this.timeMatrix = timeMatrix;

	}

	public int nbrTasks() {
		return tasks.size();
	}

	public int nbrOutputs() {
		return outputs.size();
	}

	public int nbrTrays() {
		return trays.size();
	}

	public int nbrFixtures() {
		return fixtures.size();
	}

	public int nbrMachines() {
		return machines.size();
	}

	public int nbrTools() {
		return tools.size();
	}

	public int nbrComponents() {
		return components.size();
	}

	public int nbrConcurrentGroups() {
		return concurrent.size();
	}

	public int nbrOrderedGroups() {
		return ordered.size();
	}

	/* Adding */
	public void addTray(Tray t) {
		if (!doneParsing) {
			trays.put(t.id, t);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addOutput(Output o) {
		if (!doneParsing) {
			outputs.put(o.id, o);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addFixture(Fixture f) {
		if (!doneParsing) {
			fixtures.put(f.id, f);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addComponent(Component c) {
		if (!doneParsing) {
			components.put(c.id, c);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addTool(Tool t) {
		if (!doneParsing) {
			tools.put(t.id, t);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addMachine(Machine m) {
		if (!doneParsing) {
			machines.put(m.id, m);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addTask(Task t) {
		if (!doneParsing) {
			tasks.put(t.id, t);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addOrderedGroup(ArrayList<Task> og) {
		if (!doneParsing) {
			ordered.add(og);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addConcurrentGroup(ArrayList<Task> cg) {
		if (!doneParsing) {
			concurrent.add(cg);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	public void addToolChangeDuration(Tool fromTool, Tool toTool, int duration) {
		if (!doneParsing) {
			toolChangeDurations.addDuration(fromTool, toTool, duration);
		} else {
			throw new IllegalAccessError(
					"Cannot add objects after parsing is done");
		}
	}

	/* Checking */
	public boolean trayExists(String id) {
		return trays.containsKey(id);
	}

	public boolean outputExists(String id) {
		return outputs.containsKey(id);
	}

	public boolean fixtureExists(String id) {
		return fixtures.containsKey(id);
	}

	public boolean componentExists(String id) {
		return components.containsKey(id);
	}

	public boolean toolExists(String id) {
		return tools.containsKey(id);
	}

	public boolean machineExists(String id) {
		return machines.containsKey(id);
	}

	public boolean taskExists(String id) {
		return tasks.containsKey(id);
	}

	/* Getting */
	public Tray getTray(String id) {
		return trays.get(id);
	}

	public Output getOutput(String id) {
		return outputs.get(id);
	}

	public Fixture getFixture(String id) {
		return fixtures.get(id);
	}

	public Component getComponent(String id) {
		return components.get(id);
	}

	public Tool getTool(String id) {
		return tools.get(id);
	}

	public Machine getMachine(String id) {
		return machines.get(id);
	}

	public Task getTask(String id) {
		return tasks.get(id);
	}

	public ArrayList<Task> getConcurrentGroup(int index) {
		return concurrent.get(index);
	}

	public ArrayList<Task> getOrderedGroup(int index) {
		return ordered.get(index);
	}

	public int getToolChangeDuration(Tool fromTool, Tool toTool) {
		return toolChangeDurations.getDuration(fromTool, toTool);
	}

	public void doneParsing() {
		doneParsing = true;

		/* Create cycles */

		HashMap<String, Component> tmpComponents = new HashMap<>();
		HashMap<String, Task> tmpTasks = new HashMap<>();
		ArrayList<ArrayList<Task>> tmpOrdered = new ArrayList<>();
		ArrayList<ArrayList<Task>> tmpConcurrent = new ArrayList<>();

		components.putAll(tmpComponents);
		tasks.putAll(tmpTasks);
		ordered.addAll(tmpOrdered);
		concurrent.addAll(tmpConcurrent);

		iToTask = new HashMap<>();
		int i = 1;
		// Taking
		i = addTasksWithAction(Task.ALLOWED_ACTION_TAKING, i);
		// Photo
		i = addTasksWithAction(Task.ALLOWED_ACTION_PHOTO, i);
		// Putting
		i = addTasksWithAction(Task.ALLOWED_ACTION_PUTTING, i);
		// Tapping
		i = addTasksWithAction(Task.ALLOWED_ACTION_TAPPING, i);
		// Peeling
		i = addTasksWithAction(Task.ALLOWED_ACTION_PEELING, i);
		// Mounting
		i = addTasksWithAction(Task.ALLOWED_ACTION_MOUNTING, i);
		// Moving
		i = addTasksWithAction(Task.ALLOWED_ACTION_MOVING, i);
		// Spraying
		i = addTasksWithAction(Task.ALLOWED_ACTION_SPRAYING, i);

		Iterator<Task> taskIds = tasks.values().iterator();
		// Output
		while (taskIds.hasNext()) {
			Task t = taskIds.next();
			if (t.output != null) {
				t.i = i;
				i++;
				iToTask.put(t.i, t);
			}
		}

		iToOutput = new HashMap<>();
		Iterator<Entry<String, Output>> outputIds = outputs.entrySet()
				.iterator();
		i = 1;
		while (outputIds.hasNext()) {
			Output o = outputIds.next().getValue();
			o.i = i;
			i++;
			iToOutput.put(o.i, o);
		}

		iToTray = new HashMap<>();
		Iterator<Entry<String, Tray>> trayIds = trays.entrySet().iterator();
		i = 1;
		while (trayIds.hasNext()) {
			Tray t = trayIds.next().getValue();
			t.i = i;
			i++;
			iToTray.put(t.i, t);
		}

		iToFixture = new HashMap<>();
		Iterator<Entry<String, Fixture>> fixtureIds = fixtures.entrySet()
				.iterator();
		i = 1;
		while (fixtureIds.hasNext()) {
			Fixture f = fixtureIds.next().getValue();
			f.i = i;
			i++;
			iToFixture.put(f.i, f);
		}

		iToComponent = new HashMap<>();
		Iterator<Entry<String, Component>> componentIds = components.entrySet()
				.iterator();
		i = 1;
		while (componentIds.hasNext()) {
			Component c = componentIds.next().getValue();
			c.i = i;
			i++;
			iToComponent.put(c.i, c);
		}

		iToTool = new HashMap<>();
		Iterator<Entry<String, Tool>> toolIds = tools.entrySet().iterator();
		i = 1;
		while (toolIds.hasNext()) {
			Tool t = toolIds.next().getValue();
			t.i = i;
			i++;
			iToTool.put(t.i, t);
		}

		iToMachine = new HashMap<>();
		Iterator<Entry<String, Machine>> machineIds = machines.entrySet()
				.iterator();
		i = 1;
		while (machineIds.hasNext()) {
			Machine m = machineIds.next().getValue();
			m.i = i;
			i++;
			iToMachine.put(m.i, m);
		}

		timeMatrix3D = new TimeMatrix3D(this, timeMatrix);
	}

	private int addTasksWithAction(String action, int i) {
		Iterator<Task> taskIds = tasks.values().iterator();
		while (taskIds.hasNext()) {
			Task t = taskIds.next();
			if (action.equals(t.action) && t.output == null) {
				t.i = i;
				i++;
				iToTask.put(t.i, t);
			}
		}
		return i;
	}

	/* Index functions */
	public Task iToTask(int i) {
		if (doneParsing) {
			return iToTask.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Output iToOutput(int i) {
		if (doneParsing) {
			return iToOutput.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Tray iToTray(int i) {
		if (doneParsing) {
			return iToTray.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Fixture iToFixture(int i) {
		if (doneParsing) {
			return iToFixture.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Component iToComponent(int i) {
		if (doneParsing) {
			return iToComponent.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Tool iToTool(int i) {
		if (doneParsing) {
			return iToTool.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

	public Machine iToMachine(int i) {
		if (doneParsing) {
			return iToMachine.get(i);
		} else {
			throw new IllegalAccessError(
					"Cannot call index functions before parsing is done");
		}
	}

}
