package runner;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.PrintWriter;

public class Runner {

	private static String model = "/home/tommy/Desktop/Master\\ Thesis/Workspace/constraintsandrobots/MiniZinc/Assembly_example_1.mzn";

	public static String run(String config) {
		StringBuilder res = new StringBuilder();

		try {
			File f = new File("config_" + System.currentTimeMillis()
					+ "_temp.dzn");
			PrintWriter writer = new PrintWriter(f);
			writer.println(config);
			writer.close();

			ProcessBuilder pb = new ProcessBuilder("mzn-g12fd", "-a", model, f
					.getAbsolutePath().replace(" ", "\\ "));
			Process p = pb.start();
			p.waitFor();
			res.append(p.exitValue());
			res.append(printErrors(p.getErrorStream()));
			BufferedReader reader = new BufferedReader(new InputStreamReader(
					p.getInputStream()));
			String line;
			while ((line = reader.readLine()) != null) {
				res.append(line).append("\n");
			}

			 f.delete();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (InterruptedException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

		return res.toString();
	}

	private static String printErrors(InputStream is) throws IOException {
		BufferedReader reader = new BufferedReader(new InputStreamReader(is));
		StringBuilder sb = new StringBuilder();
		String line;
		while ((line = reader.readLine()) != null) {
			sb.append(line).append("\n");
		}
		return sb.toString();
	}
}
