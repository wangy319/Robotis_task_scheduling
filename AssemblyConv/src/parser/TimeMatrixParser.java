package parser;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.util.ArrayList;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import timematrix.TimeMatrix;

public class TimeMatrixParser {

	public static TimeMatrix parse(File m) throws IOException {
		TimeMatrix matrix = new TimeMatrix();

		ArrayList<String> columns = new ArrayList<>();

		BufferedReader reader = new BufferedReader(new FileReader(m));

		// Read columns
		String line = reader.readLine();

		if (line.split(";")[0].length() != 0) {
			reader.close();
			throw new IllegalArgumentException(
					"The first cell needs to be empty");
		}

		Matcher matcher = Pattern.compile(";(?:([\\w \\-,]+))").matcher(line);
		while (matcher.find()) {
			if (matcher.group(1) == null) {
				columns.add(matcher.group(2));
			} else {
				columns.add(matcher.group(1));
			}
		}

		while ((line = reader.readLine()) != null) {
			matcher = Pattern.compile("(?:([\\d.]+|[\\w \\-,]+))")
					.matcher(line);
			String fromId = null;
			int toI = 0;
			while (matcher.find()) {
				try {
					int time = (int) Math.round(Double.parseDouble(matcher
							.group()));
					matrix.addTime(fromId, columns.get(toI), time);
					toI++;
				} catch (Exception e) {
					fromId = matcher.group();
				}
			}
		}

		reader.close();

		return matrix;

	}
}
