package parsers;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;

import parts.Move;
import parts.Task;

public class JacopParser {

	public static ArrayList<Task> parse(String fileName, String dataFileName)
			throws IOException {
		ArrayList<Task> out = new ArrayList<>();
		byte[] encoded = Files.readAllBytes(Paths.get(fileName));
		String content = new String(encoded, StandardCharsets.UTF_8);
		String[] solutions = content.split("\\-\\-+");
		String[] lines = solutions[solutions.length - 2].split("\n");
		ArrayList<String> moveDurations = new ArrayList<>();
		ArrayList<String> moveStarts = new ArrayList<>();
		ArrayList<String> starts = new ArrayList<>();
		ArrayList<String> ends = new ArrayList<>();
		ArrayList<String> preds = new ArrayList<>();
		ArrayList<String> usingMachines = new ArrayList<>();
		ArrayList<String> toolsUsed = new ArrayList<>();
		ArrayList<String> names = parseNames(dataFileName);

		for (String line : lines) {
			if (line.startsWith("start")) {
				starts = parseArray(line);
			} else if (line.startsWith("end")) {
				ends = parseArray(line);

			} else if (line.startsWith("moveDuration")) {
				moveDurations = parseArray(line);

			} else if (line.startsWith("moveStart")) {
				moveStarts = parseArray(line);

			} else if (line.startsWith("pred")) {
				preds = parseArray(line);

			} else if (line.startsWith("toolUsed")) {
				toolsUsed = parseArray(line);

			} else if (line.startsWith("usingMachine")) {
				usingMachines = parseArray(line);

			}
		}

		for (int i = 0; i < moveStarts.size(); i++) {
			Move moveTo = new Move(parseInt(moveStarts.get(i)),
					parseInt(moveDurations.get(i)), "Move to " + names.get(i));
			Task t = new Task(parseInt(starts.get(i)), parseInt(ends.get(i))
					- parseInt(starts.get(i)), i + 1,
					parseInt(usingMachines.get(i)), parseInt(preds.get(i)),
					parseInt(toolsUsed.get(i)), names.get(i), moveTo);
			out.add(t);
		}

		return out;
	}

	private static ArrayList<String> parseArray(String in) {
		return new ArrayList<String>(Arrays.asList(in.split("\\[")[1]
				.split("\\]")[0].split(",")));
	}

	private static int parseInt(String in) {
		return Integer.parseInt(in.trim());
	}

	private static ArrayList<String> parseNames(String dataFileName)
			throws IOException {
		BufferedReader reader = new BufferedReader(new FileReader(new File(
				dataFileName)));
		String line = null;
		while ((line = reader.readLine()) != null) {
			if (line.startsWith("name")) {
				reader.close();
				return new ArrayList<>(Arrays.asList(line.split("\\[")[1]
						.split("\\]")[0].split("\",\"")));
			}
		}
		reader.close();
		return null;
	}

}
