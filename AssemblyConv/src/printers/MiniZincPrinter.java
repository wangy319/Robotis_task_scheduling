package printers;

import java.util.ArrayList;

import objects.Assembly;
import objects.Component;
import objects.Fixture;
import objects.Machine;
import objects.Output;
import objects.Task;
import objects.Tool;
import objects.Tray;
import timematrix.TimeMatrix;
import timematrix.TimeMatrix3D;

public class MiniZincPrinter {

	public static String print(Assembly assembly, TimeMatrix matrix) {
		StringBuilder sb = new StringBuilder();

		// TODO Refactor out everything dealing with cycles

		sb.append("% -------------- Parameters --------------- %\n\n");
		sb.append("tempFilter = true;\n\n");
		sb.append("predFilter = true;\n\n");
		sb.append("% ------------- Assembly data ------------- %\n\n");

		sb.append("nbrTasks = ").append(assembly.nbrTasks()).append(";\n");
		sb.append("nbrFixtures = ").append(assembly.nbrFixtures())
				.append(";\n");
		sb.append("nbrMachines = ").append(assembly.nbrMachines())
				.append(";\n");
		sb.append("nbrTools = ").append(assembly.nbrTools()).append(";\n");
		sb.append("nbrTrays = ").append(assembly.nbrTrays()).append(";\n");
		sb.append("nbrOutputs = ").append(assembly.nbrOutputs()).append(";\n");
		appendNames(sb, assembly);
		sb.append("\n");
		appendDurations(sb, assembly);
		sb.append("\n");
		appendTrays(sb, assembly);
		sb.append("\n");
		appendOutputs(sb, assembly);
		sb.append("\n");
		appendFixtures(sb, assembly);
		sb.append("\n");
		appendComponentsUsed(sb, assembly);
		sb.append("\n");
		appendSubcomponents(sb, assembly);
		sb.append("\n");
		appendComponentsCreated(sb, assembly);
		sb.append("\n");
		appendAction(sb, assembly, Task.ALLOWED_ACTION_PUTTING);
		sb.append("\n");
		appendAction(sb, assembly, Task.ALLOWED_ACTION_MOUNTING);
		sb.append("\n");
		appendAction(sb, assembly, Task.ALLOWED_ACTION_TAKING);
		sb.append("\n");
		appendAction(sb, assembly, Task.ALLOWED_ACTION_MOVING);
		sb.append("\n");
		appendConcurrentTasks(sb, assembly);
		sb.append("\n");
		appendTasksOutOfRange(sb, assembly);
		sb.append("\n");
		appendOrderedGroups(sb, assembly);
		sb.append("\n");
		appendTimeMatrix(sb, assembly, matrix);
		sb.append("\n");
		sb.append("% Tools").append("\n");
		for (int i = 1; i <= assembly.nbrTools(); i++) {
			sb.append("% ").append(i).append(": ")
					.append(assembly.iToTool(i).id).append("\n");
		}
		appendToolsNeeded(sb, assembly);

		return sb.toString();
	}

	private static void appendNames(StringBuilder sb, Assembly assembly) {
		sb.append("name = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			sb.append("\"").append(assembly.iToTask(i).id).append("\"");
			// if (i < assembly.nbrTasks()) {
			sb.append(",");
			// }
		}

		// Start tasks
		for (int i = 0; i < assembly.nbrMachines(); i++) {
			sb.append("\"").append("Start " + assembly.dummyTask.id + (i + 1))
					.append("\"");
			sb.append(",");
		}

		// Goal tasks
		for (int i = 0; i < assembly.nbrMachines(); i++) {
			sb.append("\"").append("Goal " + assembly.dummyTask.id + (i + 1))
					.append("\"");
			if (i < assembly.nbrMachines() - 1) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendDurations(StringBuilder sb, Assembly assembly) {
		sb.append("duration = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			sb.append(assembly.iToTask(i).duration);
			// if (i < assembly.nbrTasks()) {
			sb.append(",");
			// }
		}

		// Start task durations
		for (int i = 0; i < assembly.nbrMachines(); i++) {
			sb.append(assembly.dummyTask.duration);
			sb.append(",");
		}

		// Goal task durations
		for (int i = 0; i < assembly.nbrMachines(); i++) {
			sb.append(assembly.dummyTask.duration);
			if (i < assembly.nbrMachines() - 1) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendTrays(StringBuilder sb, Assembly assembly) {
		sb.append("% Trays").append("\n");
		for (int i = 1; i <= assembly.nbrTrays(); i++) {
			sb.append("% ").append(i).append(": ")
					.append(assembly.iToTray(i).id).append("\n");
		}

		sb.append("tray = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Tray t = assembly.iToTask(i).tray;
			if (t == null) {
				sb.append(0);
			} else {
				sb.append(t.i);
			}
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendOutputs(StringBuilder sb, Assembly assembly) {
		sb.append("% Outputs").append("\n");
		for (int i = 1; i <= assembly.nbrOutputs(); i++) {
			sb.append("% ").append(i).append(": ")
					.append(assembly.iToOutput(i).id).append("\n");
		}

		sb.append("out_put = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Output o = assembly.iToTask(i).output;
			if (o == null) {
				sb.append(0);
			} else {
				sb.append(o.i);
			}
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendFixtures(StringBuilder sb, Assembly assembly) {
		sb.append("% Fixtures").append("\n");
		for (int i = 1; i <= assembly.nbrFixtures(); i++) {
			sb.append("% ").append(i).append(": ")
					.append(assembly.iToFixture(i).id).append("\n");
		}

		sb.append("fixture = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Fixture f = assembly.iToTask(i).fixture;
			if (f == null) {
				sb.append(0);
			} else {
				sb.append(f.i);
			}
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendComponentsUsed(StringBuilder sb, Assembly assembly) {
		sb.append("% Components").append("\n");
		for (int i = 1; i <= assembly.nbrComponents(); i++) {
			sb.append("% ").append(i).append(": ")
					.append(assembly.iToComponent(i).id).append("\n");
		}

		int nbrComponents = assembly.nbrComponents();

		sb.append("componentsUsed = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			ArrayList<Component> comps = assembly.iToTask(i).componentsUsed;
			sb.append("{");
			for (int j = 0; j < comps.size(); j++) {
				sb.append(comps.get(j).i);
				if (j < comps.size() - 1) {
					sb.append(",");
				}
			}
			sb.append("}");
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}

		sb.append("];\n");
		sb.append("nbrComponents = ").append(nbrComponents).append(";\n");
	}

	private static void appendSubcomponents(StringBuilder sb, Assembly assembly) {
		sb.append("taskSubComponents = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			ArrayList<Component> comps = assembly.iToTask(i).componentsUsed;
			sb.append("{");
			for (int j = 0; j < comps.size(); j++) {
				ArrayList<Component> subComponents = comps.get(j).subcomponents;
				if (subComponents.size() > 0) {
					for (int k = 0; k < subComponents.size(); k++) {
						sb.append(subComponents.get(k).i);
						if (k < subComponents.size() - 1) {
							sb.append(",");
						}
					}
				} else {
					sb.append(comps.get(j).i);
				}
				if (j < comps.size() - 1) {
					sb.append(",");
				}

			}
			sb.append("}");
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}

		sb.append("];\n");

		sb.append("subComponents = [");
		for (int i = 1; i <= assembly.nbrComponents(); i++) {
			if (i > 1) {
				sb.append(",");
			}
			sb.append("{");
			ArrayList<Component> subcomponets = assembly.iToComponent(i).subcomponents;
			for (int j = 0; j < subcomponets.size(); j++) {
				if (j > 0) {
					sb.append(",");
				}
				sb.append(subcomponets.get(j).i);
			}
			sb.append("}");
		}
		sb.append("];\n");

		sb.append("taskCompleteSubComponents = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Task t = assembly.iToTask(i);
			ArrayList<String> subComps = new ArrayList<>(t.getSubComponents());
			sb.append("{");
			for (int j = 0; j < subComps.size(); j++) {
				sb.append(assembly.getComponent(subComps.get(j)).i);
				if (j < subComps.size() - 1) {
					sb.append(",");
				}
			}
			sb.append("}");
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendComponentsCreated(StringBuilder sb,
			Assembly assembly) {
		sb.append("componentCreated = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Component c = assembly.iToTask(i).componentCreated;
			if (c == null) {
				sb.append(0);
			} else {
				sb.append(c.i);
			}
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendAction(StringBuilder sb, Assembly assembly,
			String action) {
		sb.append("% ").append(action).append(" a component").append("\n");

		sb.append(action).append(" = {");
		boolean first = true;
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Task t = assembly.iToTask(i);
			if (t.action != null && t.action.toLowerCase().equals(action)) {
				if (!first) {
					sb.append(",");
				} else {
					first = false;
				}
				sb.append(t.i);
			}
		}
		sb.append("};\n");
	}

	private static void appendConcurrentTasks(StringBuilder sb,
			Assembly assembly) {
		int nbrConcurrentGroups = assembly.nbrConcurrentGroups();
		sb.append("concurrentTasks = [");
		for (int i = 0; i < nbrConcurrentGroups; i++) {
			ArrayList<Task> group = assembly.getConcurrentGroup(i);
			sb.append("{");
			for (int j = 0; j < group.size(); j++) {
				sb.append(group.get(j).i);
				if (j < group.size() - 1) {
					sb.append(",");
				}
			}
			sb.append("}");
			if (i < nbrConcurrentGroups - 1) {
				sb.append(",");
			}
		}

		sb.append("];\n");
		sb.append("nbrConcurrentGroups = ").append(nbrConcurrentGroups)
				.append(";\n");
	}

	private static void appendTasksOutOfRange(StringBuilder sb,
			Assembly assembly) {
		sb.append("tasksOutOfRange=[");
		for (int i = 1; i <= assembly.nbrMachines(); i++) {
			Machine m = assembly.iToMachine(i);
			ArrayList<Task> tasks = m.tasksOutOfRange;
			sb.append("{");
			for (int j = 0; j < tasks.size(); j++) {
				sb.append(tasks.get(j).i);
				if (j < tasks.size() - 1) {
					sb.append(",");
				}
			}
			sb.append("}");
			if (i < assembly.nbrMachines()) {
				sb.append(",");
			}
		}
		sb.append("];\n");
	}

	private static void appendOrderedGroups(StringBuilder sb, Assembly assembly) {
		int orderedGroups = assembly.nbrOrderedGroups();
		int maxOrderedLength = 0;

		for (int i = 0; i < orderedGroups; i++) {
			ArrayList<Task> al = assembly.getOrderedGroup(i);
			if (al.size() > maxOrderedLength) {
				maxOrderedLength = al.size();
			}
		}

		StringBuilder[] rows = new StringBuilder[orderedGroups];
		for (int i = 0; i < orderedGroups; i++) {
			rows[i] = new StringBuilder();
		}

		for (int i = 0; i < orderedGroups; i++) {
			ArrayList<Task> group = assembly.getOrderedGroup(i);
			for (int j = 0; j < maxOrderedLength; j++) {
				if (j >= group.size()) {
					rows[i].append(0);
				} else {
					rows[i].append(group.get(j).i);
				}
				if (j < maxOrderedLength - 1) {
					rows[i].append(",");
				}
			}
		}

		sb.append("order = [|");
		for (int i = 0; i < rows.length; i++) {
			sb.append(rows[i]).append("|");
			if (i < rows.length - 1) {
				sb.append("\n");
			}
		}
		if (rows.length == 0) {
			sb.append("|");
		}
		sb.append("];\n");
		sb.append("maxOrderedLength = ").append(maxOrderedLength).append(";\n");
		sb.append("orderedGroups = ").append(orderedGroups).append(";\n");
	}

	private static void appendTimeMatrix(StringBuilder sb, Assembly assembly,
			TimeMatrix matrix) {

		append3DTimeMatrix(sb, assembly);

	}

	private static void append3DTimeMatrix(StringBuilder sb, Assembly assembly) {
		int nbrI = assembly.nbrTasks();
		int nbrJ = nbrI;
		TimeMatrix3D timeMatrix3D = assembly.timeMatrix3D;
		int nbrK = timeMatrix3D.nbrK();
		sb.append("timeMatrix3D = array3d(").append("1..")
				.append(nbrI + assembly.nbrMachines()).append(",1..")
				.append(nbrJ).append(",1..").append(nbrK).append(",[");

		for (int i = 1; i <= nbrI; i++) {
			String from = assembly.iToTask(i).id;
			for (int j = 1; j <= nbrJ; j++) {
				String to = assembly.iToTask(j).id;
				for (int k = 0; k < nbrK; k++) {
					sb.append(timeMatrix3D.getTime(from, to, k)).append(",");
				}
			}
		}

		for (int m = 0; m < assembly.nbrMachines(); m++) {
			String from = "Start";
			for (int j = 1; j <= nbrJ; j++) {
				String to = assembly.iToTask(j).id;
				for (int k = 0; k < nbrK; k++) {
					sb.append(timeMatrix3D.getTime(from, to, k));
					if (k < nbrK - 1 || m < assembly.nbrMachines()) {
						sb.append(",");
					}
				}
			}
		}
		sb.append("]);\n");
		sb.append("timeMatrix3DDepth = ").append(nbrK).append(";\n");
	}

	private static void appendToolsNeeded(StringBuilder sb, Assembly assembly) {
		sb.append("toolNeeded = [");
		for (int i = 1; i <= assembly.nbrTasks(); i++) {
			Tool t = assembly.iToTask(i).toolNeeded;
			if (t == null) {
				sb.append(0);
			} else {
				sb.append(t.i);
			}
			if (i < assembly.nbrTasks()) {
				sb.append(",");
			}
		}

		// For start tasks
		for (int goalTasks = 0; goalTasks < assembly.nbrMachines(); goalTasks++) {
			sb.append(",").append(0);
		}

		sb.append("];\n");
	}
}
