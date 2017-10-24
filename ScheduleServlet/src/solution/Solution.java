package solution;

public class Solution {

	public static String process(String result) {
		String solution = extractSolution(result);

		StringBuilder sb = new StringBuilder();
		sb.append(solution);
		return sb.toString();
	}

	private static String extractSolution(String result) {
		String[] parts = result.split("\\-\\-+");
		if (parts.length < 2) {
			return result;
		}
		return parts[parts.length - 2];
	}
}
