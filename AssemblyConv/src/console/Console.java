package console;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.nio.file.Path;
import java.nio.file.Paths;

import javax.xml.parsers.ParserConfigurationException;
import javax.xml.stream.XMLStreamException;

import objects.Assembly;

import org.xml.sax.SAXException;

import parser.TimeMatrixParser;
import parser.XMLParser;
import printers.MiniZincPrinter;
import timematrix.TimeMatrix;

public class Console {

	private static String usage = "This program can be used to convert an XML assembly instructions file and a CSV time matrix file to a MiniZinc data file\n"
			+ "Usage: AssemblyConv [TIME MATRIX FILE] [XML FILE]";

	public static void main(String[] args) throws SAXException, IOException,
			ParserConfigurationException, XMLStreamException {
		if (args.length == 0) {
			System.out.println(usage);
		} else if (!args[0].endsWith(".csv")) {
			System.out.println(args[0]);
			System.out.println("Must specify a CSV file");
			System.out.println(usage);
		} else if (!args[1].endsWith(".xml")) {
			System.out.println(args[1]);
			System.out.println("Must specify a XML file");
			System.out.println(usage);
		} else {
			Path path = Paths.get(args[1]);
			String[] tmp = path.getFileName().toString().split("\\.");
			String filename = tmp[0];

			TimeMatrix matrix = TimeMatrixParser.parse(new File(args[0]));

			Assembly assembly = XMLParser.parse(
					new File(args[args.length - 1]), matrix);

			String out = MiniZincPrinter.print(assembly, matrix);

			String mzFilename = filename + "_minizinc.dzn";
			PrintWriter mzFile = new PrintWriter(new File(path.toAbsolutePath()
					.getParent().toString()
					+ "/" + mzFilename));
			mzFile.print(out);
			mzFile.close();
		}
	}
}
