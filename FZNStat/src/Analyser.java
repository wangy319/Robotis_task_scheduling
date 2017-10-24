import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class Analyser {
	public static String analyse(File file) throws IOException {
		BufferedReader reader = new BufferedReader(new FileReader(file));
		String line;

		int nbrVar = 0;
		int nbrIntVar = 0;
		int nbrBoolVar = 0;
		int nbrArrays = 0;

		ConstraintMap cm = new ConstraintMap();

		while ((line = reader.readLine()) != null) {
			if (line.matches("var \\d+.*")) {
				nbrIntVar++;
			} else if (line.matches("var bool.*")) {
				nbrBoolVar++;
			} else if (line.matches("array.*")) {
				nbrArrays++;
			} else if (line.matches("constraint.*")) {
				Matcher m = Pattern.compile("constraint ([\\w_]+)\\(.*")
						.matcher(line);
				if (m.matches()) {
					cm.addConstraint(m.group(1));
				}
			}
		}

		reader.close();

		nbrVar = nbrIntVar + nbrBoolVar;

		StringBuilder out = new StringBuilder();

		out.append("Statistics for ").append(file.getName()).append("\n");
		out.append("----------- Variables -----------").append("\n");
		out.append("Number of integer variables: ").append(nbrIntVar)
				.append("\n");
		out.append("Number of boolean variables: ").append(nbrBoolVar)
				.append("\n");
		out.append("Total number of variables: ").append(nbrVar).append("\n");
		out.append("Number of arrays: ").append(nbrArrays).append("\n");

		out.append("----------- Constraints -----------").append("\n");
		HashMap<String, Integer> constraints = cm.getConstraints();
		for (String constraint : constraints.keySet()) {
			out.append("\t").append(constraint).append(": ")
					.append(constraints.get(constraint)).append("\n");
		}
		out.append("Total number of constraints: ")
				.append(cm.getTotalNbrOfConstraints()).append("\n");
		out.append("Of which are reified: ").append(cm.getPercentRefied())
				.append("%");

		return out.toString();
	}

	private static class ConstraintMap {
		private HashMap<String, Integer> constraints;

		public ConstraintMap() {
			constraints = new HashMap<>();
		}

		public void addConstraint(String constraint) {
			if (constraints.containsKey(constraint)) {
				int tmp = constraints.get(constraint) + 1;
				constraints.put(constraint, tmp);
			} else {
				constraints.put(constraint, 1);
			}
		}

		public HashMap<String, Integer> getConstraints() {
			return constraints;
		}

		public int getTotalNbrOfConstraints() {
			int total = 0;
			for (int i : constraints.values()) {
				total += i;
			}
			return total;
		}

		public int getNbrReifConstraints() {
			int total = 0;
			for (String constraint : constraints.keySet()) {
				if (constraint.contains("_reif")) {
					total += constraints.get(constraint);
				}
			}
			return total;
		}

		public double getPercentRefied() {
			return Math.floor(((double) getNbrReifConstraints())
					/ getTotalNbrOfConstraints() * 10000) / 100;
		}
	}
}
