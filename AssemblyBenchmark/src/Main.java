import java.io.BufferedReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.PrintWriter;

import javax.xml.parsers.ParserConfigurationException;
import javax.xml.stream.XMLStreamException;

import org.xml.sax.SAXException;

import console.Console;

public class Main {
	public static void main(String[] args) throws SAXException, IOException,
			ParserConfigurationException, XMLStreamException,
			InterruptedException {
		args = new String[] {
				"-m",
				"-t",
				"/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model5/time_matrix.csv",
				"/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model5/XML/xml_in.xml" };
		String file = "/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model5/XML/xml_in_minizinc.dzn";
		PrintWriter writer;
		while (true) {
			Console.main(args);
			long time = runTest(file);
			writer = new PrintWriter(new FileWriter("output", true));
			writer.append(time + "ms\n");
			writer.close();
		}
	}

	private static long runTest(String file) throws IOException,
			InterruptedException, SAXException, ParserConfigurationException,
			XMLStreamException {
		long start, stop;
		Process p = Runtime
				.getRuntime()
				.exec(new String[] {
						"mzn2fzn",
						"-I",
						"/home/tommy/Desktop/Master Thesis/Workspace/Solvers/jacop/mznlib/",
						"-d",
						file,
						"/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model5/model.mzn" });
		p.waitFor();
		start = System.currentTimeMillis();
		p = Runtime
				.getRuntime()
				.exec(new String[] {
						"java",
						"-Xprof",
						"-cp",
						".:/home/tommy/Desktop/Master Thesis/Workspace/Solvers/jacop-4.1.0.jar",
						"org.jacop.fz.Fz2jacop", "-s", "-a", "model.fzn" });
		p.waitFor();
		stop = System.currentTimeMillis();
		return stop - start;
	}

	private static String readOutput(InputStream in) throws IOException {
		StringBuilder sb = new StringBuilder();
		String line;
		BufferedReader reader = new BufferedReader(new InputStreamReader(in));
		while ((line = reader.readLine()) != null) {
			sb.append(line).append("\n");
		}
		return sb.toString();
	}
}
