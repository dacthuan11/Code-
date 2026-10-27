@(5000..5006) | ForEach-Object {
  try {
    $r = Invoke-WebRequest "http://localhost:$_/health" -UseBasicParsing -TimeoutSec 2 -EA Stop
    "[:$_] OK $($r.StatusCode)"
  } catch {
    "[:$_] DOWN"
  }
}
