package timematrix;

import java.util.HashMap;

import objects.Assembly;

public class TimeMatrix3D {
	HashMap<String, HashMap<String, HashMap<Integer, Integer>>> matrix;

	public TimeMatrix3D(Assembly assembly, TimeMatrix timeMatrix) {
		matrix = new HashMap<>();
		createMatrix(assembly, timeMatrix);
	}

	private void createMatrix(Assembly assembly, TimeMatrix timeMatrix) {

		String from = "Start";
		addToMatrix(from, assembly, timeMatrix);

		for (int row = 1; row <= assembly.nbrTasks(); row++) {
			from = assembly.iToTask(row).id;
			addToMatrix(from, assembly, timeMatrix);
		}
	}

	private void addToMatrix(String fromId, Assembly assembly,
			TimeMatrix timeMatrix) {
		for (int column = 1; column <= assembly.nbrTasks(); column++) {
			String to = assembly.iToTask(column).id;
			for (int fromTool = 1; fromTool <= assembly.nbrTools(); fromTool++) {
				for (int toTool = 1; toTool <= assembly.nbrTools(); toTool++) {
					int toolDiff = Math.abs(fromTool - toTool);
					if (fromId.equals("Start")) {
					}
					if (toolDiff > 0) {
						addTime(fromId,
								to,
								toolDiff,
								assembly.timeMatrix.getTime(fromId,
										"Change tool")
										+ assembly.getToolChangeDuration(
												assembly.iToTool(fromTool),
												assembly.iToTool(toTool))
										+ assembly.timeMatrix.getTime(
												"Change tool", to));
					} else {
						addTime(fromId, to, toolDiff,
								assembly.timeMatrix.getTime(fromId, to));
					}
				}
			}
		}
	}

	public Integer getTime(String fromId, String toId, int toolDiff)
			throws IllegalAccessError {
		if (!matrix.containsKey(fromId)) {
			throw new IllegalAccessError(
					"The time matrix doesn't contain a row for " + fromId);
		}
		HashMap<String, HashMap<Integer, Integer>> row = matrix.get(fromId);
		if (!row.containsKey(toId)) {
			throw new IllegalAccessError(
					"The time matrix doesn't contain a column for \"" + toId
							+ "\"");
		}
		HashMap<Integer, Integer> cell = row.get(toId);
		if (!cell.containsKey(toolDiff)) {
			throw new IllegalAccessError(
					"The time matrix doesn't contain a tool cell for \""
							+ toolDiff + "\"");
		}
		return cell.get(toolDiff);
	}

	public void addTime(String fromId, String toId, int toolDiff, int time) {
		HashMap<String, HashMap<Integer, Integer>> row;
		if (matrix.containsKey(fromId)) {
			row = matrix.get(fromId);
		} else {
			row = new HashMap<>();
			matrix.put(fromId, row);
		}

		HashMap<Integer, Integer> cell;
		if (row.containsKey(toId)) {
			cell = row.get(toId);
		} else {
			cell = new HashMap<>();
			row.put(toId, cell);
		}
		cell.put(toolDiff, time);
	}

	@SuppressWarnings("unchecked")
	public int nbrK() {
		return ((HashMap<Integer, Integer>) ((HashMap<String, HashMap<Integer, Integer>>) matrix
				.values().toArray()[0]).values().toArray()[0]).size();
	}

}
