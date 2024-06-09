var unityDiagnostics = (function () {
  var overlayOpen = false;
  var intervalId = 0;
  var graph_stats;
  function openDiagnosticsDiv(GetMetricsInfoFunc) {
    // if not open, open!
    if (!overlayOpen) {
      var diagnostics_div = document.getElementById('diagnostics-overlay');
      if (!diagnostics_div) {
        createDiagnosticsLayout();
      }

      var totalJSMemDiv = document.getElementById("jsTotalMem");
      var usedJSMemDiv = document.getElementById("jsUsedMem");
      var totalWASMHeapDiv = document.getElementById("wasmTotalMem");
      var usedWASMHeapDiv = document.getElementById("wasmUsedMem");
      var pageLoadTimeDiv = document.getElementById("pageLoadTime");
      var pageLoadTimeToFrame1Div = document.getElementById("pageLoadTimeToFrame1");
      var movingAverageFpsDiv = document.getElementById("movingAverageFps");
      var fpsDiv = document.getElementById("fps");
      var numJankedFramesDiv = document.getElementById("numJankedFrames");
      var webAssemblyStartupTimeDiv = document.getElementById("webAssemblyStartupTime");
      var codeDownloadTimeDiv = document.getElementById("codeDownloadTime");
      var gameStartupTimeDiv = document.getElementById("gameStartupTime");
      var assetLoadTimeDiv = document.getElementById("assetLoadTime");

      intervalId = setInterval(updateWebMetricInfo, 1000);
      document.getElementById("diagnostics-overlay").style.height = "20%";
      diagnostics_icon.style.filter = "grayscale(1)";
      overlayOpen = true;
    }

    function createDiagnosticsLayout() {
      diagnostics_div = document.createElement("div");
      diagnostics_div.id = "diagnostics-overlay";

      document.body.appendChild(diagnostics_div);

      var diagnostics_btn = document.createElement("div");
      diagnostics_btn.id = "diagnostics-btn";
      diagnostics_btn.innerHTML = "X";
      diagnostics_btn.addEventListener("click", closeOverlay);

      diagnostics_div.appendChild(diagnostics_btn);

      var diagnostics_summary = document.createElement("div");
      diagnostics_summary.id = "diagnostics-summary";

      var diagnostics_graph = document.createElement("div");
      diagnostics_graph.id = "diagnostics-graph";

      diagnostics_div.appendChild(diagnostics_summary);
      diagnostics_div.appendChild(diagnostics_graph);

      createDiagnosticsRow(diagnostics_summary, "Total JS Memory", "jsTotalMem", false);
      createDiagnosticsRow(diagnostics_summary, "Used JS Memory", "jsUsedMem", true);
      createDiagnosticsRow(diagnostics_summary, "Total WASM Heap", "wasmTotalMem", false);
      createDiagnosticsRow(diagnostics_summary, "Used WASM Heap", "wasmUsedMem", true);
      createDiagnosticsRow(diagnostics_summary, "Page Load Time To First Frame", "pageLoadTimeToFrame1", false);
      createDiagnosticsRow(diagnostics_summary, "Page Load Time", "pageLoadTime", true);
      createDiagnosticsRow(diagnostics_summary, "Code Download Time", "codeDownloadTime", true);
      createDiagnosticsRow(diagnostics_summary, "Load time of asset file(.data)", "assetLoadTime", true);
      createDiagnosticsRow(diagnostics_summary, "WebAssembly Startup Time", "webAssemblyStartupTime", true);
      createDiagnosticsRow(diagnostics_summary, "Game Startup Time", "gameStartupTime", true);
      createDiagnosticsRow(diagnostics_summary, "Average FPS (10 seconds)", "movingAverageFps", false);
      createDiagnosticsRow(diagnostics_summary, "Current frames per second", "fps", true);
      createDiagnosticsRow(diagnostics_summary, "Number of Frame Stalls", "numJankedFrames", false);

      graph_stats = new createGraph();
      diagnostics_graph.appendChild(graph_stats.graph_dom);
    }

    function createDiagnosticsRow(diagnostics_summary, labelInnerHtml, spanId, indent) {
      var data_row = document.createElement("div");
      data_row.className = "data-row";
      diagnostics_summary.appendChild(data_row);

      var label = document.createElement("div");
      label.className = "label";

      var data = document.createElement("div");
      data.className = "data";
      data_row.appendChild(data);

      var span = document.createElement("span");
      data.appendChild(span);

      label.innerHTML = labelInnerHtml;
      span.id = spanId;
      if (indent) {
        var indent = document.createElement("div");
        indent.className = "indented";
        data_row.appendChild(indent);
        indent.appendChild(label);
      } else {
        data_row.append(label);
      }
      data_row.append(data);
    }

    function updateWebMetricInfo(){
      var metricsInfo = GetMetricsInfoFunc();
      if (isNaN(metricsInfo.totalJSHeapSize) && isNaN(metricsInfo.usedJSHeapSize)) {
        totalJSMemDiv.textContent = "N/A";
        usedJSMemDiv.textContent = "N/A";
      } else {
        totalJSMemDiv.textContent = formatBytes(metricsInfo.totalJSHeapSize);
        usedJSMemDiv.textContent = formatBytes(metricsInfo.usedJSHeapSize);
      }
      totalWASMHeapDiv.textContent = formatBytes(metricsInfo.totalWASMHeapSize);
      usedWASMHeapDiv.textContent = formatBytes(metricsInfo.usedWASMHeapSize);
      pageLoadTimeDiv.textContent = (metricsInfo.pageLoadTime/1000).toFixed(2) + ' sec';
      pageLoadTimeToFrame1Div.textContent = (metricsInfo.pageLoadTimeToFrame1/1000).toFixed(2) + ' sec';
      assetLoadTimeDiv.textContent = (metricsInfo.assetLoadTime/1000).toFixed(2) + ' sec';
      webAssemblyStartupTimeDiv.textContent = (metricsInfo.webAssemblyStartupTime/1000).toFixed(2) + ' sec';
      codeDownloadTimeDiv.textContent = (metricsInfo.codeDownloadTime/1000).toFixed(2) + ' sec';
      gameStartupTimeDiv.textContent = (metricsInfo.gameStartupTime/1000).toFixed(2) + ' sec';
      movingAverageFpsDiv.textContent = (metricsInfo.movingAverageFps).toFixed(2);
      fpsDiv.textContent = (metricsInfo.fps).toFixed(2);
      numJankedFramesDiv.textContent = metricsInfo.numJankedFrames;

      graph_stats.plotGraph(metricsInfo, true);
    }

    function closeOverlay() {
      clearInterval(intervalId);
      intervalId = 0;
      document.getElementById("diagnostics-overlay").style.height = "0px";
      diagnostics_icon.style.filter = "grayscale(0)";
      overlayOpen = false;
    }

    function formatNumber(num) {
      num = num || 0;
      var num_str = num.toString();
      if (num >= 1000) return num_str.substr(0, 4);
      else return num_str.substr(0, 5);
    }

    function formatBytes(bytes) {
      if (bytes >= 1024 * 1024 * 1024) return formatNumber(bytes / (1024 * 1024 * 1024)) + '\xa0GB';
      else if (bytes >= 1024 * 1024) return formatNumber(bytes / (1024 * 1024)) + '\xa0MB';
      else if (bytes >= 1024) return formatNumber(bytes / 1024) + '\xa0KB';
      else return formatNumber(bytes) + ' B';
    }

    function formatBytesInfo(bytes) {
      if (bytes >= 1024 * 1024 * 1024) return {
        bytesValue: formatNumber(bytes / (1024 * 1024 * 1024)),
        unitMeasure: 'GB'
      };
      else if (bytes >= 1024 * 1024) return {
        bytesValue: formatNumber(bytes / (1024 * 1024)),
        unitMeasure: 'MB'
      };
      else if (bytes >= 1024) return {
        bytesValue: formatNumber(bytes / 1024),
        unitMeasure: 'KB'
      };
      else return {
        bytesValue: formatNumber(bytes),
        unitMeasure: 'B'
      };
    }

    function GraphPanel(name, fontColor, backgroundColor) {
      var min = Infinity, max = 0, round = Math.round;

      var pixel_ratio = round( window.devicePixelRatio || 1);
      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
        pixel_ratio = 1;
      }
    
      var canvas_width = 250 * pixel_ratio, canvas_height = 180 * pixel_ratio,
          textname_xpos = 6 * pixel_ratio, textname_ypos = 6 * pixel_ratio,
          graph_area_xpos = 8 * pixel_ratio, graph_area_ypos = 30 * pixel_ratio,
          graph_area_width = 234 * pixel_ratio, graph_area_height = 140 * pixel_ratio;
    
      var graph_canvas = document.createElement('canvas');
      graph_canvas.id = "diagnostics-graph-canvas";
      graph_canvas.width = canvas_width;
      graph_canvas.height = canvas_height;
    
      var graph_ctx = graph_canvas.getContext('2d');
      graph_ctx.font = 'bold ' + ( 16 * pixel_ratio) + 'px Helvetica,Arial,sans-serif';
      graph_ctx.textBaseline = 'top';
    
      graph_ctx.fillStyle = backgroundColor;
      graph_ctx.fillRect( 0, 0, canvas_width, canvas_height);

      graph_ctx.fillStyle = fontColor;
      graph_ctx.fillText( name, textname_xpos, textname_ypos);
      graph_ctx.fillRect( graph_area_xpos, graph_area_ypos, graph_area_width, graph_area_height);
    
      
      graph_ctx.fillStyle = backgroundColor;
      graph_ctx.globalAlpha = 0.5;
      graph_ctx.fillRect( graph_area_xpos, graph_area_ypos, graph_area_width, graph_area_height);

      return {
        graph_dom: graph_canvas,
        update: function (value, unitMeasure) {
          min = Math.min(min, value);
          max = Math.max(max, value);
          
          graph_ctx.fillStyle = backgroundColor;
          graph_ctx.globalAlpha = 1;
          graph_ctx.fillRect(0, 0, canvas_width, graph_area_ypos);
          
          graph_ctx.fillStyle = fontColor;
          graph_ctx.textAlign = 'left';
          graph_ctx.fillText(name, textname_xpos, textname_ypos);

          graph_ctx.textAlign = 'right';
          if(unitMeasure){
            graph_ctx.fillText(formatNumber(value) + unitMeasure + ' (' + round( min ) + '-' + round( max ) + ')', canvas_width-10, textname_ypos);
          }
          else{
            graph_ctx.fillText(formatNumber(value) + ' (' + round( min ) + '-' + round( max ) + ')', canvas_width-10, textname_ypos);
          }

          graph_ctx.drawImage(graph_canvas, graph_area_xpos + pixel_ratio, graph_area_ypos, graph_area_width - pixel_ratio, graph_area_height, graph_area_xpos, graph_area_ypos, graph_area_width - pixel_ratio, graph_area_height);
    
          graph_ctx.fillRect(graph_area_xpos + graph_area_width - pixel_ratio, graph_area_ypos, pixel_ratio, graph_area_height);
    
          graph_ctx.fillStyle = backgroundColor;
          graph_ctx.globalAlpha = 0.5;
          graph_ctx.fillRect(graph_area_xpos + graph_area_width - pixel_ratio, graph_area_ypos, pixel_ratio, round((1 - (value / max)) * graph_area_height));
        }
      };
    }

    function createGraph(){
      var numGraphPanel = 4;
      var graphContainer = document.createElement('div');
      if(graphContainer === undefined) return;

      graphContainer.style.cssText = 'cursor:pointer;';
      graphContainer.style.display = 'contents';


      function addGraphPanel(graph_panel) {
        graphContainer.appendChild(graph_panel.graph_dom);
        return graph_panel;
      }

      function showGraphPanel(id) {
        graphContainer.children[id].style.margin = '1em';
      }
      var fpsGraphPanel = addGraphPanel(new GraphPanel('FPS', '#0ff', '#002'));
      var movingAvgFpsGraphPanel = addGraphPanel(new GraphPanel('Average FPS', '#0f0', '#020'));
      var usedJsMemGraphPanel = addGraphPanel(new GraphPanel('Used JS Mem', '#f60', '#201'));
      var usedWasmMemGraphPanel = addGraphPanel(new GraphPanel('Used Wasm', '#ff0', '#020'));

      for(var i=0;i<numGraphPanel;i++){
        showGraphPanel(i);
      }

      return {
        graph_dom: graphContainer,
        plotGraph: function (metrics) {
          fpsGraphPanel.update(metrics.fps);
          movingAvgFpsGraphPanel.update(metrics.movingAverageFps);

          var formattedBytesInfo = formatBytesInfo(metrics.usedWASMHeapSize);
          usedWasmMemGraphPanel.update(formattedBytesInfo.bytesValue, formattedBytesInfo.unitMeasure);
          if(!isNaN(metrics.usedJSHeapSize)){
            formattedBytesInfo = formatBytesInfo(metrics.usedJSHeapSize);
            usedJsMemGraphPanel.update(formattedBytesInfo.bytesValue, formattedBytesInfo.unitMeasure); 
          } 
        }
      };
    }
  }

  return {
    openDiagnosticsDiv: openDiagnosticsDiv
  };

})();
