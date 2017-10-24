package containers;

import java.util.HashMap;

import objects.Tool;

public class ToolChangeDurations {
	HashMap<Tool, HashMap<Tool, Integer>> matrix;

	public ToolChangeDurations() {
		matrix = new HashMap<>();
	}

	public void addDuration(Tool fromTool, Tool toTool, int duration) {
		HashMap<Tool, Integer> row;
		if (!matrix.containsKey(fromTool)) {
			row = new HashMap<>();
		} else {
			row = matrix.get(fromTool);
		}
		row.put(toTool, duration);
		matrix.put(fromTool, row);
	}

	public int getDuration(Tool fromTool, Tool toTool) {
		if (matrix.containsKey(fromTool)) {
			if (matrix.get(fromTool).containsKey(toTool)) {
				return matrix.get(fromTool).get(toTool);
			}
		}
		return 0;

	}

}
