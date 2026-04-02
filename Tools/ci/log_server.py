#!/usr/bin/env python3
"""
Simple log receiver for the Snake Game remote logger.

Run this on your dev machine, then set LOG_SERVER_URL in the tvOS app
to point to this server. All Debug.Log output from the game will appear here.

Usage:
    python3 Tools/ci/log_server.py
    # Then in another terminal, set the URL in the app or use:
    # curl -X POST http://localhost:8080 -d '{"level":"Log","message":"test"}'
"""
from http.server import HTTPServer, BaseHTTPRequestHandler
import json
import datetime
import sys

COLORS = {
    "Log": "\033[37m",       # white
    "Warning": "\033[33m",   # yellow
    "Error": "\033[31m",     # red
    "Exception": "\033[31;1m",  # bold red
    "Assert": "\033[35m",    # magenta
}
RESET = "\033[0m"


class LogHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(length).decode("utf-8")
        timestamp = datetime.datetime.now().strftime("%H:%M:%S.%f")[:-3]

        try:
            data = json.loads(body)
            level = data.get("level", "Log")
            message = data.get("message", body)
            game_time = data.get("time", 0)
            color = COLORS.get(level, "")
            print(f"{color}[{timestamp} t={game_time:.1f}] [{level}] {message}{RESET}")
            if data.get("stack"):
                for line in data["stack"].strip().split("\n")[:5]:
                    print(f"  {line}")
        except json.JSONDecodeError:
            print(f"[{timestamp}] {body[:500]}")

        # Write to file as well
        with open("game_logs.jsonl", "a") as f:
            f.write(json.dumps({"time": timestamp, "data": body}) + "\n")

        self.send_response(200)
        self.end_headers()

    def log_message(self, format, *args):
        pass  # Suppress default HTTP logging


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    print(f"Log server listening on http://0.0.0.0:{port}")
    print(f"Set LOG_SERVER_URL=http://YOUR_IP:{port} in the app")
    print("---")
    HTTPServer(("0.0.0.0", port), LogHandler).serve_forever()
