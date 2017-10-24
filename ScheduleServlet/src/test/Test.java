package test;

import parser.XMLParser;
import runner.Runner;
import solution.Solution;

public class Test {

	public static void main(String[] args) {
		String config = XMLParser.mockParse("");
		System.out.println(config);
		System.out.println();
		String result = Runner.run(config);
		System.out.println(result);
		System.out.println();
		System.out.println(Solution.process(result));
	}

}
