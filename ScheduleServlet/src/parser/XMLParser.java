package parser;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.List;

public class XMLParser {

	public static String parse(String xml) {
		StringBuilder out = new StringBuilder();

		return out.toString();
	}

	public static String mockParse(String xml) {
		StringBuilder out = new StringBuilder();
		try {
			List<String> lines = Files
					.readAllLines(
							Paths.get("/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Assembly_example_1.dzn"),
							Charset.defaultCharset());

			for (String line : lines) {
				out.append(line).append("\n");
			}
		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		return out.toString();
	}

}
