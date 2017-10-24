import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.util.ArrayList;

import parser.TimeMatrixParser;
import timematrix.TimeMatrix;

public class Main {
	public static void main(String[] args) throws IOException {
		TimeMatrix timeMarix = TimeMatrixParser
				.parse(new File(
						"/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model5/time_matrix.csv"));
		ArrayList<ArrayList<Integer>> toolChangeDur = parseToolChangeDur(new File(
				"ToolChangeDur"));
		int nbrTools = toolChangeDur.size();
	}

	private static ArrayList<ArrayList<Integer>> parseToolChangeDur(File f)
			throws IOException {
		ArrayList<ArrayList<Integer>> dur = new ArrayList<>();

		BufferedReader reader = new BufferedReader(new FileReader(f));

		String line;

		while ((line = reader.readLine()) != null) {
			String[] row = line.split(",");
			ArrayList<Integer> r = new ArrayList<>();
			for (String s : row) {
				r.add(Integer.parseInt(s));
			}
			dur.add(r);
		}
		reader.close();

		return dur;
	}
}
