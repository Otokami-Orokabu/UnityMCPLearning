// Unity モックデータ（テスト用）

export interface UnityGameObject {
  name: string;
  type: string;
  position: {
    x: number;
    y: number;
    z: number;
  };
}

export interface UnityProjectInfo {
  editorVersion: string;
  projectName: string;
  sceneName: string;
  platform: string;
  gameObjects: UnityGameObject[];
  assetCount: number;
  isPlaying: boolean;
  buildTarget: string;
  timestamp: string;
}

// モックデータの生成
export function generateMockUnityData(): UnityProjectInfo {
  return {
    editorVersion: '2023.3.0f1',
    projectName: 'UnityMCPTestProject',
    sceneName: 'SampleScene',
    platform: 'Standalone',
    gameObjects: [
      {
        name: 'Main Camera',
        type: 'Camera',
        position: { x: 0, y: 1, z: -10 }
      },
      {
        name: 'Directional Light',
        type: 'Light',
        position: { x: 0, y: 3, z: 0 }
      },
      {
        name: 'Cube',
        type: 'MeshRenderer',
        position: { x: 0, y: 0, z: 0 }
      }
    ],
    assetCount: 42,
    isPlaying: false,
    buildTarget: 'StandaloneOSX',
    timestamp: new Date().toISOString()
  };
}

// ランダムな変化を加えたモックデータ（動的テスト用）
export function generateDynamicMockData(): UnityProjectInfo {
  const baseData = generateMockUnityData();

  return {
    ...baseData,
    isPlaying: Math.random() > 0.5,
    assetCount: 40 + Math.floor(Math.random() * 10),
    gameObjects: baseData.gameObjects.map(obj => ({
      ...obj,
      position: {
        x: obj.position.x + (Math.random() - 0.5) * 2,
        y: obj.position.y + (Math.random() - 0.5) * 2,
        z: obj.position.z + (Math.random() - 0.5) * 2
      }
    })),
    timestamp: new Date().toISOString()
  };
}