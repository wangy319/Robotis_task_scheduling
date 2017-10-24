package scheduler;

import java.io.IOException;
import java.io.PrintWriter;

import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import parser.XMLParser;
import runner.Runner;
import solution.Solution;

public class Scheduler extends HttpServlet {

	/**
	 * 
	 */
	private static final long serialVersionUID = -862580820499670794L;

	public void init() throws ServletException {
		// Do required initialization
	}

	public void doGet(HttpServletRequest request, HttpServletResponse response)
			throws ServletException, IOException {
		String config = XMLParser.mockParse("");
		String result = Runner.run(config);
		String solution = Solution.process(result);
		// Set response content type
		response.setContentType("text/html");
		// Actual logic goes here.
		PrintWriter out = response.getWriter();
		out.println(result);
	}

	public void destroy() {
		// do nothing.
	}

}
