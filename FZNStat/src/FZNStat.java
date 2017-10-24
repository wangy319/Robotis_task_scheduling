import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

public class FZNStat {

	public static void main(String[] args) throws IOException {

		File folder = new File(".");

		for (File file : folder.listFiles()) {
			if (file.getName().endsWith(".fzn")) {

				String out = Analyser.analyse(file);

				PrintWriter writer = new PrintWriter(new File(file.getName()
						.split("\\.")[0] + "_stat"));
				writer.print(out);
				writer.close();
			}
		}
	}
}
