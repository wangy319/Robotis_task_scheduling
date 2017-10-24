package parsers;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import parts.Move;
import parts.Task;

public class G12Parser {
	public static ArrayList<Task> parse(String fileName) throws IOException {
		ArrayList<Task> out = new ArrayList<>();
		byte[] encoded = Files.readAllBytes(Paths.get(fileName));
		String content = new String(encoded, StandardCharsets.UTF_8);
		String[] parts = content.split("\\-\\-+");
		if (parts.length >= 2) {
			Matcher m = Pattern
					.compile(
							"\n(\\d+)\\s(\\d+)\\s(\\d+)\\s(\\d+)\\s(\\d+)\\s(\\d+)\\s([\\w \\-,]+)")
					.matcher(parts[parts.length - 2]);
			Move moveTo = null;
			while (m.find()) {
				System.out.println(m.group(7));
				if (m.group(7).startsWith("Move from")) {
					moveTo = new Move(Integer.parseInt(m.group(1)),
							Integer.parseInt(m.group(2)), m.group(7));
				} else {
					out.add(new Task(Integer.parseInt(m.group(1)), Integer
							.parseInt(m.group(2)),
							Integer.parseInt(m.group(3)), Integer.parseInt(m
									.group(4)), Integer.parseInt(m.group(5)),
							Integer.parseInt(m.group(6)), m.group(7), moveTo));
				}
			}
		}
		return out;
	}
}
