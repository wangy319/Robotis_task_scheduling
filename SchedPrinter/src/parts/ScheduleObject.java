package parts;

public abstract class ScheduleObject {
	public int start;
	public int duration;
	public String name;

	public ScheduleObject(int start, int duration, String name) {
		this.start = start;
		this.duration = duration;
		this.name = name;
	}

	public String print(int width) {
		String text;
		text = getText();
		StringBuilder sb = new StringBuilder();
		sb.append(getLine(width));
		sb.append("\n");
		sb.append("|");
		int charCount = 0;
		for (int i = 0; i < text.length(); i++) {
			sb.append(text.charAt(i));
			charCount++;
			if (i == text.length() - 1) {
				for (int j = 0; j < width - charCount - 2; j++) {
					sb.append(" ");
				}
				sb.append("|");
				break;
			}
			if (charCount == width - 2) {
				sb.append("|\n|");
				charCount = 0;
			}
		}
		sb.append("\n");
		sb.append(getLine(width));
		return sb.toString();
	}

	public abstract String getText();

	private String getLine(int width) {
		StringBuilder sb = new StringBuilder("+");
		for (int i = 0; i < width - 2; i++) {
			sb.append('-');
		}
		sb.append("+");
		return sb.toString();
	}
}
