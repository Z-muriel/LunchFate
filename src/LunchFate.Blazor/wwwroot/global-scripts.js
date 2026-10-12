/* 高德地图 JS API 前端模块（FR-M-06）。
 * 由 Blazor 组件通过 IJSRuntime 的 import 按需加载，不注册全局变量，也不再进入 MVC 脚本包。
 * 无 Key 或加载失败时由调用方降级显示地址，不阻断页面。 */

let amapPromise;
const mapInstances = new Map();

/** 浏览器本地默认搜索中心的存储键。 */
const DEFAULT_CENTER_KEY = "lunchfate.map.defaultCenter";

/**
 * 读取浏览器本地保存的默认搜索中心。
 * @returns {{longitude: number, latitude: number}|null}
 */
export function getDefaultCenter() {
    try {
        const raw = window.localStorage.getItem(DEFAULT_CENTER_KEY);
        if (!raw) {
            return null;
        }

        const parsed = JSON.parse(raw);
        if (typeof parsed?.longitude !== "number" || typeof parsed?.latitude !== "number") {
            return null;
        }

        return { longitude: parsed.longitude, latitude: parsed.latitude };
    } catch (e) {
        return null;
    }
}

/**
 * 把当前搜索中心保存为浏览器本地默认位置。
 * @param {{longitude: number, latitude: number}} center 搜索中心
 * @returns {boolean} 是否保存成功（隐私模式等场景 localStorage 可能不可用）
 */
export function setDefaultCenter(center) {
    try {
        window.localStorage.setItem(DEFAULT_CENTER_KEY, JSON.stringify({
            longitude: center.longitude,
            latitude: center.latitude
        }));
        return true;
    } catch (e) {
        return false;
    }
}

/**
 * 清除浏览器本地默认位置，之后回退到服务端配置的默认位置。
 * @returns {boolean} 是否清除成功
 */
export function clearDefaultCenter() {
    try {
        window.localStorage.removeItem(DEFAULT_CENTER_KEY);
        return true;
    } catch (e) {
        return false;
    }
}

/**
 * 加载高德 JS API 脚本，重复调用复用同一个加载中的 Promise。
 * @param {string} key 高德 Web端(JS API) Key
 * @param {string} [securityJsCode] 安全密钥；2021-12-02 之后申请的 Key 需要，缺失会报 INVALID_USER_SCODE
 * @returns {Promise<void>}
 */
export function loadAmap(key, securityJsCode) {
    // 安全密钥必须在脚本加载前写入全局配置，否则高德不会读取。
    if (securityJsCode) {
        window._AMapSecurityConfig = { securityJsCode: securityJsCode };
    }

    if (window.AMap) {
        return Promise.resolve();
    }

    if (amapPromise) {
        return amapPromise;
    }

    amapPromise = new Promise(function (resolve, reject) {
        const script = document.createElement("script");
        script.src = "https://webapi.amap.com/maps?v=2.0&key=" + encodeURIComponent(key);
        script.async = true;
        script.onload = function () {
            if (window.AMap) {
                resolve();
            } else {
                amapPromise = undefined;
                reject(new Error("高德地图脚本已加载但未注册 AMap 全局对象"));
            }
        };
        script.onerror = function () {
            amapPromise = undefined;
            reject(new Error("高德地图脚本加载失败，请检查网络或 Key 的域名白名单"));
        };
        document.head.appendChild(script);
    });

    return amapPromise;
}

/**
 * 获取浏览器当前位置。
 * @returns {Promise<{longitude: number, latitude: number}>}
 */
export function getCurrentPosition() {
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            reject(new Error("浏览器不支持定位"));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function (position) {
                resolve({
                    longitude: position.coords.longitude,
                    latitude: position.coords.latitude
                });
            },
            function (error) {
                reject(new Error("无法获取当前位置：" + error.message));
            },
            { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 }
        );
    });
}

/**
 * 在指定容器渲染标点地图，每次调用重建该容器上的实例，避免多个组件互相覆盖。
 * @param {string} containerId 地图容器元素 id
 * @param {Array} markers 标点数组，元素含 longitude / latitude / name
 * @param {object} [dotNetReference] 需要接收地图点击回调时的 DotNetObjectReference
 */
export function renderShopMap(containerId, markers, dotNetReference) {
    const container = document.getElementById(containerId);
    if (!container) {
        throw new Error("地图容器不存在：" + containerId);
    }

    if (!window.AMap) {
        throw new Error("高德地图 JS API 未加载");
    }

    if (!markers || markers.length === 0) {
        throw new Error("没有可渲染的标点");
    }

    const previous = mapInstances.get(containerId);
    if (previous) {
        previous.destroy();
        mapInstances.delete(containerId);
    }

    // 清理已从文档移除的容器（Blazor 用 @key 重建组件时会换掉容器 id），避免实例残留。
    mapInstances.forEach(function (instance, id) {
        if (!document.getElementById(id)) {
            instance.destroy();
            mapInstances.delete(id);
        }
    });

    // Blazor 序列化的属性名可能是 camelCase 或 PascalCase，两种都接受。
    const getValue = function (item, name) {
        const value = item[name];
        if (value !== undefined && value !== null) {
            return value;
        }
        return item[name.charAt(0).toUpperCase() + name.slice(1)];
    };

    const first = markers[0];
    const map = new AMap.Map(container, {
        zoom: 16,
        center: [getValue(first, "longitude"), getValue(first, "latitude")],
        resizeEnable: true
    });

    const mapMarkers = markers.map(function (item) {
        return new AMap.Marker({
            position: [getValue(item, "longitude"), getValue(item, "latitude")],
            title: getValue(item, "name") || ""
        });
    });

    map.add(mapMarkers);
    if (mapMarkers.length > 1) {
        map.setFitView(mapMarkers);
    }

    if (dotNetReference) {
        map.on("click", function (event) {
            dotNetReference.invokeMethodAsync("SelectMarkerAsync", {
                longitude: event.lnglat.getLng(),
                latitude: event.lnglat.getLat(),
                name: "地图选点"
            });
        });
    }

    mapInstances.set(containerId, map);
}
