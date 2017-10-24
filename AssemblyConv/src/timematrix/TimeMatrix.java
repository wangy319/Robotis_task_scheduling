package timematrix;

import java.util.HashMap;

public class TimeMatrix {

	HashMap<String, HashMap<String, Integer>> matrix;

	public TimeMatrix() {
		matrix = new HashMap<>();
	}

	public Integer getTime(String fromId, String toId) {
		if (!matrix.containsKey(fromId)) {
			throw new IllegalAccessError(
					"The time matrix doesn't contain a row for " + fromId);
		}
		HashMap<String, Integer> tmp = matrix.get(fromId);
		if (!tmp.containsKey(toId)) {
			throw new IllegalAccessError(
					"The time matrix doesn't contain a column for \"" + toId
							+ "\"");
		}
		return tmp.get(toId);
	}

	public void addTime(String fromId, String toId, int time) {
		HashMap<String, Integer> tmp;
		if (matrix.containsKey(fromId)) {
			tmp = matrix.get(fromId);
		} else {
			tmp = new HashMap<>();
			matrix.put(fromId, tmp);
		}
		tmp.put(toId, time);
	}

}
